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
	using System.Threading;

	partial class Exceptions : BaseTest
	{
		[CustomUnityTest]
		public IEnumerator AllFinallyHandlersAreCalledOnException()
		{
			using (var test = StartTest())
			{
				var task = new FuncTask<string>(test.TaskManager, () => { throw new InvalidOperationException(); });
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

			}
		}

		[CustomUnityTest]
		public IEnumerator ContinueAfterException()
		{
			using (var test = StartTest())
			{

				var expected = new[] { typeof(InvalidOperationException).Name, typeof(InvalidCastException).Name, typeof(ArgumentNullException).Name };

				var runOrder = new List<string>();
				var exceptions = new List<Exception>();

				var task = test.TaskManager.With(() => throw new InvalidOperationException())
						   .Catch(e => {
							   runOrder.Add("1");
							   exceptions.Add(e);
							   return true;
						   })
						   .Then(() => throw new InvalidCastException())
						   .Catch(e => {
							   runOrder.Add("2");
							   exceptions.Add(e);
							   return true;
						   })
						   .Then(() => throw new ArgumentNullException())
						   .Catch(e => {
							   runOrder.Add("3");
							   exceptions.Add(e);
							   return true;
						   })
						   .Finally((s, e) => { });

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				CollectionAssert.AreEqual(expected, exceptions.Select(x => x.GetType().Name).ToArray());
				CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, runOrder);

			}
		}

		[CustomUnityTest]
		public IEnumerator ExceptionPropagatesOutIfNoFinally()
		{
			using (var test = StartTest())
			{

				var task = test.TaskManager.With(() => throw new InvalidOperationException())
				.Catch(_ => { });

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			}
		}

		[CustomUnityTest]
		public IEnumerator MultipleCatchStatementsCanHappen()
		{
			using (var test = StartTest())
			{

				var expected = new[] {
				typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name
			};
				var runOrder = new List<string>();
				var exceptions = new List<Exception>();

				var task = test.TaskManager.With(() => throw new InvalidOperationException())
						   .Catch(e => {
							   runOrder.Add("1");
							   exceptions.Add(e);
						   })
						   .Then(() => throw new InvalidCastException())
						   .Catch(e => {
							   runOrder.Add("2");
							   exceptions.Add(e);
						   })
						   .Then(() => throw new ArgumentNullException())
						   .Catch(e => {
							   runOrder.Add("3");
							   exceptions.Add(e);
						   })
						   .Finally((b, e) => { });

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				CollectionAssert.AreEqual(expected, exceptions.Select(x => x.GetType().Name).ToArray());
				CollectionAssert.AreEqual(new string[] { "1", "2", "3" }, runOrder);

			}
		}

		[CustomUnityTest]
		public IEnumerator StartAndEndAreAlwaysRaised()
		{
			using (var test = StartTest())
			{

				var runOrder = new List<string>();

				ITask task = test.TaskManager.With(() => throw new Exception());
				task.OnStart += _ => runOrder.Add("start");
				task.OnEnd += (_, __, ___) => runOrder.Add("end");

				// we want to run a Finally on a new task (and not in-thread) so that the StartAndSwallowException handler runs after this
				// one, proving that the exception is propagated after everything is done
				task = task.Finally((_, __) => { });

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				var expected = new[] { "start", "end" };
				CollectionAssert.AreEqual(expected, runOrder);

			}
		}

	}
}
