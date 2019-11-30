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
	partial class Exceptions : BaseTest
	{
		[CustomUnityTest]
		public IEnumerator AllFinallyHandlersAreCalledOnException()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var task = new FuncTask<string>(taskManager, () => { throw new InvalidOperationException(); });
			bool exceptionThrown1, exceptionThrown2;
			exceptionThrown1 = exceptionThrown2 = false;

			task.FinallyInline(success => exceptionThrown1 = !success);
			task.FinallyInline((success, _) => exceptionThrown2 = !success);

			var waitTask = task.Start().Task;
			var aggregateTask = Task.WhenAny(waitTask, Task.Delay(Timeout));
			foreach (var frame in WaitForCompletion(aggregateTask)) yield return frame;
			var ret = aggregateTask.Result;

			Assert.AreEqual(ret, waitTask);
			Assert.IsTrue(exceptionThrown1);
			Assert.IsTrue(exceptionThrown2);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator ContinueAfterException()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var expected = new[] { typeof(InvalidOperationException).Name, typeof(InvalidCastException).Name, typeof(ArgumentNullException).Name };

			var runOrder = new List<string>();
			var exceptions = new List<Exception>();

			var task = new ActionTask(taskManager, _ => throw new InvalidOperationException())
					   .Catch(e => {
						   runOrder.Add("1");
						   exceptions.Add(e);
						   return true;
					   })
					   .Then(_ => throw new InvalidCastException())
					   .Catch(e => {
						   runOrder.Add("2");
						   exceptions.Add(e);
						   return true;
					   })
					   .Then(_ => throw new ArgumentNullException())
					   .Catch(e => {
						   runOrder.Add("3");
						   exceptions.Add(e);
						   return true;
					   })
					   .Finally((s, e) => {});

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			CollectionAssert.AreEqual(expected, exceptions.Select(x => x.GetType().Name).ToArray());
			CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, runOrder);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator ExceptionPropagatesOutIfNoFinally()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var task = new ActionTask(taskManager, _ => throw new InvalidOperationException())
				.Catch(_ => {});

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator MultipleCatchStatementsCanHappen()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var expected = new[] {
				typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name
			};
			var runOrder = new List<string>();
			var exceptions = new List<Exception>();

			var task = new ActionTask(taskManager, _ => throw new InvalidOperationException())
					   .Catch(e => {
						   runOrder.Add("1");
						   exceptions.Add(e);
					   })
					   .Then(_ => throw new InvalidCastException())
					   .Catch(e => {
						   runOrder.Add("2");
						   exceptions.Add(e);
					   })
					   .Then(_ => throw new ArgumentNullException())
					   .Catch(e => {
						   runOrder.Add("3");
						   exceptions.Add(e);
					   })
					   .Finally((b, e) => {});

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			CollectionAssert.AreEqual(expected, exceptions.Select(x => x.GetType().Name).ToArray());
			CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, runOrder);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator StartAndEndAreAlwaysRaised()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var runOrder = new List<string>();

			ITask task = new ActionTask(taskManager, _ => { throw new Exception(); });
			task.OnStart += _ => runOrder.Add("start");
			task.OnEnd += (_, __, ___) => runOrder.Add("end");

			// we want to run a Finally on a new task (and not in-thread) so that the StartAndSwallowException handler runs after this
			// one, proving that the exception is propagated after everything is done
			task = task.Finally((_, __) => {});

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			var expected = new[] { "start", "end" };
			CollectionAssert.AreEqual(expected, runOrder);

			StopTest(watch, logger, taskManager);
		}

	}
}
