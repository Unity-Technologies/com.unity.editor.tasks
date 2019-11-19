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
	partial class SchedulerTests : BaseTest
	{
		[CustomUnityTest]
		public IEnumerator ChainingOnDifferentSchedulers()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var output = new Dictionary<int, KeyValuePair<int, int>>();
			var tasks = new List<ITask>();
			var uiThread = taskManager.UIThread;

			for (int i = 1; i < 100; i++)
			{
				tasks.Add(GetTask(taskManager, TaskAffinity.UI, i, id => {
					lock (output) output.Add(id, KeyValuePair.Create(Thread.CurrentThread.ManagedThreadId, -1));
				}).Then(GetTask(taskManager, i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i, id => {
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

			StopTest(watch, logger, taskManager);
		}

		/// <summary>
		/// This exemplifies that running a bunch of tasks that don't depend on anything on the concurrent (default) scheduler
		/// run in any order
		/// </summary>
		[CustomUnityTest]
		public IEnumerator ConcurrentSchedulerDoesNotGuaranteeOrdering()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var runningOrder = new List<int>();
			var rand = new Random(RandomSeed);
			var tasks = new List<ActionTask>();
			for (int i = 1; i < 11; i++)
			{
				tasks.Add(GetTask(taskManager, TaskAffinity.Concurrent, i, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				}));
			}

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(tasks)) yield return frame;

			Assert.AreEqual(10, runningOrder.Count);

			StopTest(watch, logger, taskManager);
		}

		/// <summary>
		/// This exemplifies that running a bunch of tasks that depend on other things on the concurrent (default) scheduler
		/// run in dependency order. Each group of tasks depends on a task on the previous group, so the first group
		/// runs first, then the second group of tasks, then the third. Run order within each group is not guaranteed
		/// </summary>
		[CustomUnityTest]
		public IEnumerator ConcurrentSchedulerWithDependencyOrdering()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var count = 3;
			var runningOrder = new List<int>();
			var rand = new Random(RandomSeed);
			var startTasks = new List<ActionTask>();
			for (var i = 0; i < count; i++)
			{
				startTasks.Add(GetTask(taskManager, TaskAffinity.Concurrent, i + 1, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				}));
			}

			var midTasks = new List<ActionTask>();
			for (var i = 0; i < count; i++)
			{
				var previousTask = startTasks[i];
				midTasks.Add(previousTask.Then(GetTask(taskManager, TaskAffinity.Concurrent, i + 11, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				})));
				;
			}

			var endTasks = new List<ActionTask>();
			for (var i = 0; i < count; i++)
			{
				var previousTask = midTasks[i];
				endTasks.Add(previousTask.Then(GetTask(taskManager, TaskAffinity.Concurrent, i + 21, id => {
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

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator ExclusiveSchedulerGuaranteesOrdering()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var runningOrder = new List<int>();
			var tasks = new List<ActionTask>();
			var rand = new Random(RandomSeed);
			for (int i = 1; i < 11; i++)
			{
				tasks.Add(GetTask(taskManager, TaskAffinity.Exclusive, i, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				}));
			}

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(tasks)) yield return frame;

			Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator NonUITasksAlwaysRunOnDifferentThreadFromUITasks()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var output = new Dictionary<int, int>();
			var tasks = new List<ITask>();
			var uiThread = Thread.CurrentThread.ManagedThreadId;

			for (int i = 1; i < 100; i++)
			{
				tasks.Add(GetTask(taskManager, i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i, id => {
					lock (output) output.Add(id, Thread.CurrentThread.ManagedThreadId);
				}).Start());
			}

			// wait for the tasks to finish
			foreach (var frame in WaitForCompletion(tasks)) yield return frame;

			CollectionAssert.DoesNotContain(output.Values, uiThread);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator UISchedulerGuaranteesOrdering()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var runningOrder = new List<int>();
			var tasks = new List<ActionTask>();
			var rand = new Random(RandomSeed);
			for (int i = 1; i < 11; i++)
			{
				tasks.Add(GetTask(taskManager, TaskAffinity.UI, i, id => {
					new ManualResetEventSlim().Wait(rand.Next(100, 200));
					lock (runningOrder) runningOrder.Add(id);
				}));
			}

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(tasks)) yield return frame;

			Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);

			StopTest(watch, logger, taskManager);
		}
	}
}
