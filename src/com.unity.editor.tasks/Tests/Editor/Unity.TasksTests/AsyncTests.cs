using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseTests;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace ThreadingTests
{
	using System;

	partial class AsyncTests : BaseTest
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
			using (var test = StartTest())
			{
				var runOrder = new List<string>();
				var task = test.TaskManager.WithExclusiveAsync(() => SetDataExclusiveAsync(test.TaskManager, runOrder, "ran"));

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				Assert.IsTrue(task.Successful, "Task did not complete successfully");

				CollectionAssert.AreEqual(new[] { $"ran" }, runOrder);
			}
		}

		[CustomUnityTest]
		public IEnumerator Inlining()
		{
			using (var test = StartTest())
			{
				var runOrder = new List<string>();
				var task = test.TaskManager.With(() => runOrder.Add($"started"), "Inlining 1")
						   .ThenAsync(() => Task.FromResult(1), TaskAffinity.Exclusive, "Inlining 2")
						   .Then(n => n + 1, "Inlining 3")
						   .Then(n => runOrder.Add(n.ToString()), "Inlining 4")
						   .ThenAsync(() => Task.FromResult(20f), TaskAffinity.Exclusive, "Inlining 5")
						   .Then(n => n + 1, "Inlining 6")
						   .Then(n => runOrder.Add(n.ToString()), "Inlining 7")
						   .Finally((s, _) => runOrder.Add("done"));

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				CollectionAssert.AreEqual(new[] { "started", "2", "21", "done" }, runOrder);
			}
		}
	}
}
