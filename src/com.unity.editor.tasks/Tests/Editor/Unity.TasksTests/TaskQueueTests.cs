using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using BaseTests;
using Unity.Editor.Tasks.Extensions;
using Unity.Editor.Tasks;

namespace ThreadingTests
{
	partial class TaskQueueTests : BaseTest
	{
		[Test]
		public void ConvertsTaskResultsCorrectly()
		{
			using (var test = StartTest())
			{

				var vals = new string[] { "2.1", Math.PI.ToString(), "1" };
				var expected = new double[] { 2.1, Math.PI, 1.0 };
				var queue = new TaskQueue<string, double>(test.TaskManager, task => Double.Parse(task.Result));
				vals.All(s => {
					queue.Queue(new TPLTask<string>(test.TaskManager, () => Task.FromResult<string>(s)));
					return true;
				});
				var ret = queue.RunSynchronously();
				Assert.AreEqual(expected.Join(","), ret.Join(","));

			}
		}

		[Test]
		public void DoesNotThrowIfItCanConvert()
		{
			using (var test = StartTest())
			{

				Assert.DoesNotThrow(() => new TaskQueue<DownloadTask, ITask>(test.TaskManager));

			}
		}

		[CustomUnityTest]
		public IEnumerator DoubleSchedulingStartsOnlyOnce()
		{
			using (var test = StartTest())
			{

				var runOrder = new List<string>();
				var queue = new TaskQueue(test.TaskManager);
				var task1 = new FuncTask<string>(test.TaskManager, () => {
					runOrder.Add("1");
					return "2";
				});
				task1.OnStart += _ => runOrder.Add("start 1");
				task1.OnEnd += (a, b, c, d) => runOrder.Add("end 1");
				var task2 = new FuncTask<string, string>(test.TaskManager, (_, str) => {
					runOrder.Add(str);
					return "3";
				});
				task2.OnStart += _ => runOrder.Add("start 2");
				task2.OnEnd += (a, b, c, d) => runOrder.Add("end 2");
				var task3 = new FuncTask<string, string>(test.TaskManager, (_, str) => {
					runOrder.Add(str);
					return "4";
				});
				task3.OnStart += _ => runOrder.Add("start 3");
				task3.OnEnd += (a, b, c, d) => runOrder.Add("end 3");

				queue.Queue(task1.Then(task2).Then(task3));

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(queue)) yield return frame;


				var expected = new string[] { "start 1", "1", "end 1", "start 2", "2", "end 2", "start 3", "3", "end 3", };
				Assert.AreEqual(expected.Join(","), runOrder.Join(","));


			}
		}

		[Test]
		public void FailingTasksThrowCorrectlyEvenIfFinallyIsPresent()
		{
			using (var test = StartTest())
			{

				var queue = new TaskQueue(test.TaskManager);
				var task = new ActionTask(test.TaskManager, () => throw new InvalidOperationException()).Finally((s, e) => { });
				queue.Queue(task);
				Assert.Throws<InvalidOperationException>(() => queue.RunSynchronously());

			}
		}

		[Test]
		public void ThrowsIfCannotConvert()
		{
			using (var test = StartTest())
			{

				Assert.Throws<ArgumentNullException>(() => new TaskQueue<int, double>(test.TaskManager));

			}
		}
	}
}
