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
			StartTest(out var watch, out var logger, out var taskManager);

			var task = new ActionTask(taskManager, () => { });

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			var task2 = new TestActionTask(taskManager, _ => { });
			var task3 = new TestActionTask(taskManager, _ => { });

			task.Then(task2).Then(task3);

			var top = task3.Test_GetFirstStartableTask();

			Assert.AreSame(null, top);

			StopTest(watch, logger, taskManager);
		}

		[Test]
		public void GetFirstStartableTask_ReturnsTopTaskWhenNotStarted()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var task1 = new ActionTask(taskManager, () => { });
			var task2 = new TestActionTask(taskManager, _ => { });
			var task3 = new TestActionTask(taskManager, _ => { });

			task1.Then(task2).Then(task3);

			var top = task3.Test_GetFirstStartableTask();
			Assert.AreSame(task1, top);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator GetTopOfChain_ReturnsTopMostInCreatedState()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var task = new ActionTask(taskManager, () => { });

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			var task2 = new TestActionTask(taskManager, _ => { });
			var task3 = new TestActionTask(taskManager, _ => { });

			task.Then(task2).Then(task3);

			var top = task3.GetTopOfChain();
			Assert.AreSame(task2, top);

			StopTest(watch, logger, taskManager);
		}

		[Test]
		public void GetTopOfChain_ReturnsTopTaskWhenNotStarted()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var task1 = new TPLTask(taskManager, () => Task.FromResult(true));
			var task2 = new TestActionTask(taskManager, _ => { });
			var task3 = new TestActionTask(taskManager, _ => { });

			task1.Then(task2).Then(task3);

			var top = task3.GetTopOfChain();
			Assert.AreSame(task1, top);

			StopTest(watch, logger, taskManager);
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
			StartTest(out var watch, out var logger, out var taskManager);

			var callOrder = new List<string>();
			var dependsOrder = new List<ITask>();

			ITask innerChainTask1;
			ITask<string> innerChainTask2;
			ITask<string> innerChainTask3;

			ITask<int> outerChainTask1;
			ITask<string> outerChainTask2;
			ITask outerChainTask3;

			Action<ITask> onStart = t => logger.Trace($"OnStart {t.Name}");
			Action<ITask, bool, Exception> onEnd = (t, _, __) => logger.Trace($"OnEnd {t.Name}");
			Action<ITask<string>, string, bool, Exception> onEndString = (t, _, __, ___) => logger.Trace($"OnEnd {t.Name}");
			Action<ITask<int>, int, bool, Exception> onEndInt = (t, _, __, ___) => logger.Trace($"OnEnd {t.Name}");


			innerChainTask1 = new TPLTask(taskManager, () => Task.FromResult(true)) { Name = nameof(innerChainTask1) };
			innerChainTask1.OnStart += t => {
				onStart(t);
				callOrder.Add(nameof(innerChainTask1));
			};
			innerChainTask1.OnEnd += onEnd;

			innerChainTask2 = innerChainTask1.Then(_ => {
				callOrder.Add(nameof(innerChainTask2));
				return "1";
			}, nameof(innerChainTask2));
			innerChainTask2.OnStart += onStart;
			innerChainTask2.OnEnd += onEndString;

			innerChainTask3 = innerChainTask2.Finally((s, e, d) => {
				callOrder.Add(nameof(innerChainTask3));
				return d;
			}, nameof(innerChainTask3));
			innerChainTask3.OnStart += onStart;
			innerChainTask3.OnEnd += onEndString;

			outerChainTask1 = new FuncTask<int>(taskManager, _ => {
				callOrder.Add(nameof(outerChainTask1));
				return 1;
			}) { Name = nameof(outerChainTask1) };
			outerChainTask1.OnStart += onStart;
			outerChainTask1.OnEnd += onEndInt;

			outerChainTask2 = outerChainTask1.Then(innerChainTask3);
			outerChainTask2.OnStart += onStart;
			outerChainTask2.OnEnd += onEndString;

			outerChainTask3 = outerChainTask2.Finally((s, e) => callOrder.Add(nameof(outerChainTask3)), nameof(outerChainTask3));
			outerChainTask3.OnStart += onStart;
			outerChainTask3.OnEnd += onEnd;

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

			StopTest(watch, logger, taskManager);
		}
	}
}
