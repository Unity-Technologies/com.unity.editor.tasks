using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BaseTests;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace ThreadingTests
{
	using System.Threading.Tasks;

	partial class SchedulerTests : BaseTest
	{
		[Test]
		public void CustomScheduler_ThrowsIfNotSet()
		{
			using (var test = StartTest())
			{
				var task = test.TaskManager.With(() => {}, TaskAffinity.Custom);
				Assert.Throws<InvalidOperationException>(() => task.Start());
			}
		}

		[CustomUnityTest]
		public IEnumerator CustomScheduler_Works()
		{
			using (var test = StartTest())
			{
				using (var context = new ThreadSynchronizationContext(test.TaskManager.Token))
				using (var scheduler = new SynchronizationContextTaskScheduler(context))
				{
					int expected = 0;
					scheduler.Context.Send(_ => expected = Thread.CurrentThread.ManagedThreadId, null);

					var task = test.TaskManager.With(() => Thread.CurrentThread.ManagedThreadId, TaskAffinity.Custom);
					task.Start(scheduler);

					foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

					Assert.Greater(task.Result, 1);
					task.Result.Matches(expected);
				}
			}
		}

		[CustomUnityTest]
		public IEnumerator CustomScheduler_AsyncKeepsOrder()
		{
			using (var test = StartTest())
			{
				using (var context = new ThreadSynchronizationContext(test.TaskManager.Token))
				using (var scheduler = new SynchronizationContextTaskScheduler(context))
				{
					int expected = 0;
					scheduler.Context.Send(_ => expected = Thread.CurrentThread.ManagedThreadId, null);

					var order = new List<int>();
					var task1 = test.TaskManager
					                .WithAsync(async s => {
						                await Task.Delay(10);
						                s.Add(1);
						                return s;
					                }, order, TaskAffinity.Custom)
					                .Finally((s, e, ret) => {
						                if (!s) e.Rethrow();
						                return ret;
					                });

					var task2 = test.TaskManager.WithAsync(async s => {
						                await Task.Yield();
						                s.Add(2);
						                return s;
					                }, order, TaskAffinity.Custom)
					                .Finally((s, e, ret) => {
						                if (!s) e.Rethrow();
						                return ret;
					                });


					var task3 = test.TaskManager.WithAsync(async s => {
						                s.Add(3);
						                await Task.Yield();
						                return s;
					                }, order, TaskAffinity.Custom)
					                .Finally((s, e, ret) => {
						                if (!s) e.Rethrow();
						                return ret;
					                });

					task1.Start(scheduler);
					task2.Start(scheduler);
					task3.Start(scheduler);
					foreach (var frame in WaitForCompletion(task1, task2, task3)) yield return frame;

					order.Matches(new int[] { 1, 2, 3 });
				}
			}
		}

		[CustomUnityTest]
		public IEnumerator CustomScheduler_ChainRunsOnTheSameScheduler()
		{
			using (var test = StartTest())
			{
				using (var context = new ThreadSynchronizationContext(test.TaskManager.Token))
				using (var scheduler = new SynchronizationContextTaskScheduler(context))
				{
					int expected = 0;
					scheduler.Context.Send(_ => expected = Thread.CurrentThread.ManagedThreadId, null);

					var task = test.TaskManager
					               .With(s => {
						               s.Add(Thread.CurrentThread.ManagedThreadId);
						               return s;
					               }, new List<int>(), TaskAffinity.Custom)
					               .ThenInUI(s => {
						               s.Add(Thread.CurrentThread.ManagedThreadId);
						               return s;
					               })
					               .Then(s => {
						               s.Add(Thread.CurrentThread.ManagedThreadId);
						               return s;
					               }, TaskAffinity.Custom)
					               .Finally((s, e, ret) => {
						               if (!s) e.Rethrow();
						               return ret;
					               });

					task.Start(scheduler);
					foreach (var frame in WaitForCompletion(task)) yield return frame;

					if (!task.Successful)
					{
						task.Exception.Rethrow();
					}
					var actual = task.Result;
					actual.Matches(new[] { expected, test.TaskManager.UIThread, expected });
				}
			}
		}

		[CustomUnityTest]
		public IEnumerator ChainingOnDifferentSchedulers()
		{
			using (var test = StartTest())
			{

				var output = new Dictionary<int, KeyValuePair<int, int>>();
				var tasks = new List<ITask>();
				var uiThread = test.TaskManager.UIThread;

				for (int i = 1; i < 100; i++)
				{
					tasks.Add(GetTask(test.TaskManager, TaskAffinity.UI, i, id => {
						lock (output) output.Add(id, KeyValuePair.Create(Thread.CurrentThread.ManagedThreadId, -1));
					}).Then(GetTask(test.TaskManager, i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i, id => {
						lock (output) output[id] = KeyValuePair.Create(output[id].Key, Thread.CurrentThread.ManagedThreadId);
					}))
						  .Start());
				}

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(tasks)) yield return frame;

				foreach (var t in output)
				{
					Assert.AreEqual(uiThread, t.Value.Key,
						$"Task {t.Key} pass 1 should have been on ui thread {uiThread} but ran instead on {t.Value.Key}");
					Assert.AreNotEqual(t.Value.Key, t.Value.Value, $"Task {t.Key} pass 2 should not have been on ui thread {uiThread}");
				}

			}
		}

		/// <summary>
		/// This exemplifies that running a bunch of tasks that don't depend on anything on the concurrent (default) scheduler
		/// run in any order
		/// </summary>
		[CustomUnityTest]
		public IEnumerator ConcurrentSchedulerDoesNotGuaranteeOrdering()
		{
			using (var test = StartTest())
			{

				var runningOrder = new List<int>();
				var rand = new Random(RandomSeed);
				var tasks = new List<ActionTask>();
				for (int i = 1; i < 11; i++)
				{
					tasks.Add(GetTask(test.TaskManager, TaskAffinity.Concurrent, i, id => {
						new ManualResetEventSlim().Wait(rand.Next(100, 200));
						lock (runningOrder) runningOrder.Add(id);
					}));
				}

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(tasks)) yield return frame;

				Assert.AreEqual(10, runningOrder.Count);

			}
		}

		/// <summary>
		/// This exemplifies that running a bunch of tasks that depend on other things on the concurrent (default) scheduler
		/// run in dependency order. Each group of tasks depends on a task on the previous group, so the first group
		/// runs first, then the second group of tasks, then the third. Run order within each group is not guaranteed
		/// </summary>
		[CustomUnityTest]
		public IEnumerator ConcurrentSchedulerWithDependencyOrdering()
		{
			using (var test = StartTest())
			{

				var count = 3;
				var runningOrder = new List<int>();
				var rand = new Random(RandomSeed);
				var startTasks = new List<ActionTask>();
				for (var i = 0; i < count; i++)
				{
					startTasks.Add(GetTask(test.TaskManager, TaskAffinity.Concurrent, i + 1, id => {
						new ManualResetEventSlim().Wait(rand.Next(100, 200));
						lock (runningOrder) runningOrder.Add(id);
					}));
				}

				var midTasks = new List<ActionTask>();
				for (var i = 0; i < count; i++)
				{
					var previousTask = startTasks[i];
					midTasks.Add(previousTask.Then(GetTask(test.TaskManager, TaskAffinity.Concurrent, i + 11, id => {
						new ManualResetEventSlim().Wait(rand.Next(100, 200));
						lock (runningOrder) runningOrder.Add(id);
					})));
					;
				}

				var endTasks = new List<ActionTask>();
				for (var i = 0; i < count; i++)
				{
					var previousTask = midTasks[i];
					endTasks.Add(previousTask.Then(GetTask(test.TaskManager, TaskAffinity.Concurrent, i + 21, id => {
						new ManualResetEventSlim().Wait(rand.Next(100, 200));
						lock (runningOrder) runningOrder.Add(id);
					})));
				}

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(endTasks)) yield return frame;

				Assert.True(runningOrder.IndexOf(21) > runningOrder.IndexOf(11));
				Assert.True(runningOrder.IndexOf(11) > runningOrder.IndexOf(1));
				Assert.True(runningOrder.IndexOf(22) > runningOrder.IndexOf(12));
				Assert.True(runningOrder.IndexOf(12) > runningOrder.IndexOf(2));
				Assert.True(runningOrder.IndexOf(23) > runningOrder.IndexOf(13));
				Assert.True(runningOrder.IndexOf(13) > runningOrder.IndexOf(3));

			}
		}

		[CustomUnityTest]
		public IEnumerator ExclusiveSchedulerGuaranteesOrdering()
		{
			using (var test = StartTest())
			{

				var runningOrder = new List<int>();
				var tasks = new List<ActionTask>();
				var rand = new Random(RandomSeed);
				for (int i = 1; i < 11; i++)
				{
					tasks.Add(GetTask(test.TaskManager, TaskAffinity.Exclusive, i, id => {
						new ManualResetEventSlim().Wait(rand.Next(10, 20));
						lock (runningOrder) runningOrder.Add(id);
					}));
				}

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(tasks)) yield return frame;

				Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);

			}
		}

		[CustomUnityTest]
		public IEnumerator NonUITasksAlwaysRunOnDifferentThreadFromUITasks()
		{
			using (var test = StartTest())
			{

				var output = new Dictionary<int, int>();
				var tasks = new List<ITask>();
				var uiThread = Thread.CurrentThread.ManagedThreadId;

				for (int i = 1; i < 100; i++)
				{
					tasks.Add(GetTask(test.TaskManager, i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i, id => {
						lock (output) output.Add(id, Thread.CurrentThread.ManagedThreadId);
					}).Start());
				}

				// wait for the tasks to finish
				foreach (var frame in WaitForCompletion(tasks)) yield return frame;

				CollectionAssert.DoesNotContain(output.Values, uiThread);

			}
		}

		[CustomUnityTest]
		public IEnumerator UISchedulerGuaranteesOrdering()
		{
			using (var test = StartTest())
			{

				var runningOrder = new List<int>();
				var tasks = new List<ActionTask>();
				var rand = new Random(RandomSeed);
				for (int i = 1; i < 11; i++)
				{
					tasks.Add(GetTask(test.TaskManager, TaskAffinity.UI, i, id => {
						new ManualResetEventSlim().Wait(rand.Next(100, 200));
						lock (runningOrder) runningOrder.Add(id);
					}));
				}

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(tasks)) yield return frame;

				Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);

			}
		}

		[CustomUnityTest]
		public IEnumerator AsyncAwaitTasks_ExclusiveScheduler_RunInSchedulingOrder()
		{
			using (var test = StartTest())
			{

				var runOrder = new List<string>();
				var task1 = new TPLTask(test.TaskManager, () => SetDataExclusiveAsync(test.TaskManager, runOrder, "task1")) { Name = "Task1", Affinity = TaskAffinity.Exclusive };
				var task2 = new TPLTask(test.TaskManager, () => SetDataExclusiveAsync1(test.TaskManager, runOrder, "task2")) { Name = "Task2", Affinity = TaskAffinity.Exclusive };
				var task3 = new TPLTask(test.TaskManager, () => SetDataExclusiveAsync2(test.TaskManager, runOrder, "task3")) { Name = "Task3", Affinity = TaskAffinity.Exclusive };

				task1.Start();
				task2.Start();
				task3.Start();

				// wait for the tasks to finish
				foreach (var frame in WaitForCompletion(task1, task2, task3)) yield return frame;

				CollectionAssert.AreEqual(new[] { "task1 start", "task1 then", "task1 end", "task2 start", "task2 then", "task2 end", "task3 start", "task3 then", "task3 end" }, runOrder);

			}
		}


		class TestData
		{
			public bool Done = false;
		}

		private async Task SetDataExclusiveAsync(ITaskManager taskManager, List<string> list, string data)
		{
			Assert.AreNotEqual(taskManager.UIThread, Thread.CurrentThread.ManagedThreadId, "async task ran on the main thread when it shouldn't have");

			list.Add($"{data} start");
			await Task.Delay(10);
			list.Add($"{data} then");
			await Task.Delay(10);
			list.Add($"{data} end");
		}
		private async Task<bool> SetDataExclusiveAsync1(ITaskManager taskManager, List<string> list, string data)
		{
			Assert.AreNotEqual(taskManager.UIThread, Thread.CurrentThread.ManagedThreadId, "async task ran on the main thread when it shouldn't have");
			list.Add($"{data} start");
			await Task.Delay(2);
			list.Add($"{data} then");
			await Task.Delay(3);
			list.Add($"{data} end");
			return true;
		}

		private Task SetDataExclusiveAsync2(ITaskManager taskManager, List<string> list, string data)
		{
			Assert.AreNotEqual(taskManager.UIThread, Thread.CurrentThread.ManagedThreadId, "async task ran on the main thread when it shouldn't have");
			list.Add($"{data} start");
			list.Add($"{data} then");
			list.Add($"{data} end");
			return Task.CompletedTask;
		}
	}
}
