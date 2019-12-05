using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseTests;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace ThreadingTests
{
	partial class Dependencies : BaseTest
	{
		class TestActionTask : ActionTask
		{
			public TestActionTask(ITaskManager taskManager, Action<bool> action) : base(taskManager, action)
			{ }

			public TaskBase Test_GetFirstStartableTask()
			{
				return base.GetTopMostStartableTask();
			}
		}

		private T LogAndReturnResult<T>(List<string> callOrder, string msg, T result)
		{
			callOrder.Add(msg);
			return result;
		}

		[CustomUnityTest]
		public IEnumerator GetFirstStartableTask_ReturnsNullWhenItsAlreadyStarted()
		{
			using (var test = StartTest())
			{


				var task = new ActionTask(test.TaskManager, () => { });

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				var task2 = new TestActionTask(test.TaskManager, _ => { });
				var task3 = new TestActionTask(test.TaskManager, _ => { });

				task.Then(task2).Then(task3);

				var top = task3.Test_GetFirstStartableTask();

				Assert.AreSame(null, top);

			}
		}

		[Test]
		public void GetFirstStartableTask_ReturnsTopTaskWhenNotStarted()
		{
			using (var test = StartTest())
			{

				var task1 = new ActionTask(test.TaskManager, () => { });
				var task2 = new TestActionTask(test.TaskManager, _ => { });
				var task3 = new TestActionTask(test.TaskManager, _ => { });

				task1.Then(task2).Then(task3);

				var top = task3.Test_GetFirstStartableTask();
				Assert.AreSame(task1, top);

			}
		}

		[CustomUnityTest]
		public IEnumerator GetTopOfChain_ReturnsTopMostInCreatedState()
		{
			using (var test = StartTest())
			{

				var task = new ActionTask(test.TaskManager, () => { });

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				var task2 = new TestActionTask(test.TaskManager, _ => { });
				var task3 = new TestActionTask(test.TaskManager, _ => { });

				task.Then(task2).Then(task3);

				var top = task3.GetTopOfChain();
				Assert.AreSame(task2, top);

			}
		}

		[Test]
		public void GetTopOfChain_ReturnsTopTaskWhenNotStarted()
		{
			using (var test = StartTest())
			{

				var task1 = new TPLTask(test.TaskManager, () => Task.FromResult(true));
				var task2 = new TestActionTask(test.TaskManager, _ => { });
				var task3 = new TestActionTask(test.TaskManager, _ => { });

				task1.Then(task2).Then(task3);

				var top = task3.GetTopOfChain();
				Assert.AreSame(task1, top);

			}
		}

		/***
		 * This test creates a tree as following:
		 *
		 *  +---------------+
		 *  |  outerChain1  |
		 *  +---------------+
		 *          |
		 *          v
		 *  +---------------+
		 *  |  innerChain1  |
		 *  +---------------+
		 *          |
		 *          v
		 *  +---------------+
		 *  |  innerChain2  |
		 *  +---------------+
		 *          |
		 *          v
		 *  +---------------------------------------------+
		 *  |  innerChain3/outerChainTask2 (same object)  |
		 *  +---------------------------------------------+
		 *          |
		 *          v
		 *  +----------------------------------+
		 *  |  outerChainTask3 (Finally task)  |
		 *  +----------------------------------+
		 *
		 */
		[CustomUnityTest]
		public IEnumerator MergingTwoChainsWorks()
		{
			using (var test = StartTest())
			{

				var callOrder = new List<string>();
				var dependsOrder = new List<ITask>();

				ITask innerChainTask1;
				ITask<string> innerChainTask2;
				ITask<string> innerChainTask3;

				ITask<int> outerChainTask1;
				ITask<string> outerChainTask2;
				ITask outerChainTask3;


				innerChainTask1 = test.TaskManager.With(() => Task.FromResult(true), nameof(innerChainTask1));
				innerChainTask1.OnStart += t => {
					callOrder.Add(nameof(innerChainTask1));
				};

				innerChainTask2 = innerChainTask1.Then(() => {
					callOrder.Add(nameof(innerChainTask2));
					return "1";
				}, nameof(innerChainTask2));

				innerChainTask3 = innerChainTask2.Finally((s, e, d) => {
					callOrder.Add(nameof(innerChainTask3));
					return d;
				}, nameof(innerChainTask3));


				outerChainTask1 = test.TaskManager.With(() => {
					callOrder.Add(nameof(outerChainTask1));
					return 1;
				}, nameof(outerChainTask1));


				outerChainTask2 = outerChainTask1.Then(innerChainTask3);

				outerChainTask3 = outerChainTask2.Finally((s, e) => callOrder.Add(nameof(outerChainTask3)), nameof(outerChainTask3));

				var dependsOn = outerChainTask3;
				while (dependsOn != null)
				{
					dependsOrder.Add(dependsOn);
					dependsOn = dependsOn.DependsOn;
				}

				Assert.AreEqual(innerChainTask3, outerChainTask2);

				{
					var expected = new[] { outerChainTask1, innerChainTask1, innerChainTask2, innerChainTask3, outerChainTask3 };
					CollectionAssert.AreEqual(expected, dependsOrder.Reverse<ITask>().ToArray());
				}

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(outerChainTask3)) yield return frame;

				{
					var expected = new[] { nameof(outerChainTask1), nameof(innerChainTask1), nameof(innerChainTask2), nameof(innerChainTask3), nameof(outerChainTask3) };
					callOrder.Matches(expected);
				}

			}
		}
	}
}
