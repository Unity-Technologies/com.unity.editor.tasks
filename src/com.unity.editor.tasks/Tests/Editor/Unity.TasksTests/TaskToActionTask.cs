using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseTests;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace ThreadingTests
{
	partial class TaskToActionTask : BaseTest
	{
		private async Task SetDataExclusiveAsync(ITaskManager taskManager, List<string> list, string data)
		{
			Assert.AreNotEqual(taskManager.UIThread, System.Threading.Thread.CurrentThread.ManagedThreadId, "async task ran on the main thread when it shouldn't have");

			await Task.Delay(10);
			list.Add(data);
		}

		[CustomUnityTest]
		public IEnumerator CanWrapATask()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var runOrder = new List<string>();
			var task = new TPLTask(taskManager, () => SetDataExclusiveAsync(taskManager, runOrder, "ran")) { Affinity = TaskAffinity.Exclusive };

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			Assert.IsTrue(task.Successful, "Task did not complete successfully");

			CollectionAssert.AreEqual(new[] { $"ran" }, runOrder);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator Inlining()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var runOrder = new List<string>();
			var task = new ActionTask(taskManager, _ => runOrder.Add($"started")) { Name = "Inlining 1" }
			           .Then(() => Task.FromResult(1), "Inlining 2", TaskAffinity.Exclusive)
			           .Then((_, n) => n + 1, "Inlining 3")
			           .Then((_, n) => runOrder.Add(n.ToString()), "Inlining 4")
			           .Then(() => Task.FromResult(20f), "Inlining 5", TaskAffinity.Exclusive)
			           .Then((_, n) => n + 1, "Inlining 6")
			           .Then((_, n) => runOrder.Add(n.ToString()), "Inlining 7")
			           .Finally((s, _) => runOrder.Add("done"));

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			CollectionAssert.AreEqual(new[] { "started", "2", "21", "done" }, runOrder);

			StopTest(watch, logger, taskManager);
		}
	}
}
