using System;
using System.Collections;
using System.Collections.Generic;
using BaseTests;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace ThreadingTests
{
	partial class Chains : BaseTest
	{
		[CustomUnityTest]
		public IEnumerator CatchAlwaysRunsBeforeFinally()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var success = false;
			Exception exception = null;
			Exception finallyException = null;
			var runOrder = new List<string>();
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };
			var exceptionMessage = $"{nameof(CatchAlwaysRunsBeforeFinally)} an exception";

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI, Name = $"{nameof(CatchAlwaysRunsBeforeFinally)} Task 1" }
					   .Then((s, d) => output.Add(d), $"{nameof(CatchAlwaysRunsBeforeFinally)} Task 2")
					   .Then(_ => throw new Exception(exceptionMessage))
					   .Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive, Name = $"{nameof(CatchAlwaysRunsBeforeFinally)} Task 3" })
					   .Then(new FuncTask<string, string>(taskManager, (s, d) => {
						   output.Add(d);
						   return "done";
					   }) { Name = $"{nameof(CatchAlwaysRunsBeforeFinally)} Task 4" })
					   .Catch(ex => {
						   lock(runOrder)
						   {
							   exception = ex;
							   runOrder.Add("catch");
						   }
					   })
					   .Finally((s, e, d) => {
						   lock(runOrder)
						   {
							   success = s;
							   finallyException = e;
							   runOrder.Add("finally");
						   }
						   return d;
					   });

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(exception);
			Assert.IsNotNull(finallyException);
			Assert.AreEqual(exceptionMessage, exception.Message);
			Assert.AreEqual(exceptionMessage, finallyException.Message);
			CollectionAssert.AreEqual(new List<string> { "catch", "finally" }, runOrder);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator FinallyCanAlsoNotReturnData()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var success = false;
			Exception exception = null;
			Exception finallyException = null;
			var runOrder = new List<string>();
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name", "another name", "done" };

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d)).Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive })
					   .Then((s, d) => {
						   output.Add(d);
						   return "done";
					   }).Finally((s, e, d) => {
						   lock(runOrder)
						   {
							   success = s;
							   output.Add(d);
							   finallyException = e;
							   runOrder.Add("finally");
						   }
					   });

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			Assert.IsTrue(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNull(exception);
			Assert.IsNull(finallyException);
			CollectionAssert.AreEqual(new List<string> { "finally" }, runOrder);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator FinallyCanReturnData()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var success = false;
			Exception exception = null;
			Exception finallyException = null;
			var runOrder = new List<string>();
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name", "another name", "done" };

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d)).Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive })
					   .Then((s, d) => {
						   output.Add(d);
						   return "done";
					   }).Catch(ex => {
						   lock(runOrder)
						   {
							   exception = ex;
							   runOrder.Add("catch");
						   }
					   }).Finally((s, e, d) => {
						   lock(runOrder)
						   {
							   success = s;
							   output.Add(d);
							   finallyException = e;
							   runOrder.Add("finally");
						   }
						   return d;
					   }).ThenInUI((s, d) => {
						   lock(runOrder)
						   {
							   runOrder.Add("boo");
						   }
						   return d;
					   });

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			var ret = task.Successful ? task.Result : null;

			Assert.AreEqual("done", ret);
			Assert.IsTrue(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNull(exception);
			Assert.IsNull(finallyException);
			CollectionAssert.AreEqual(new List<string> { "finally", "boo" }, runOrder);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator FinallyReportsException()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var success = false;
			Exception finallyException = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };
			var lck = new object();
			var count = 0;
			var exceptionMessage = $"{nameof(FinallyReportsException)} an exception";

			var task = new FuncTask<string>(taskManager, _ => "one name") { Name = "Task1", Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d), "Task2")
					   .Then(_ => throw new Exception(exceptionMessage), "Throwing")
					   .Then(new FuncTask<string>(taskManager, _ => "another name") { Name = "Task3", Affinity = TaskAffinity.Exclusive })
					   .ThenInUI((s, d) => output.Add(d), "Task4")
					   .Finally((s, e) => {
						   lock (lck)
						   {
							   count++;
						   }
						   success = s;
						   lock (lck)
						   {
							   count++;
						   }
						   finallyException = e;
						   lock (lck)
						   {
							   count++;
						   }
					   });

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			lock(lck)
			{
				Assert.AreEqual(3, count);
			}

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(finallyException);
			Assert.AreEqual(exceptionMessage, finallyException.Message);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator ThrowingInterruptsTaskChainButAlwaysRunsFinallyAndCatch()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var success = false;
			string thrown = "";
			Exception finallyException = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d)).Then(_ => throw new Exception("an exception")).Catch(ex => thrown = ex.Message)
					   .Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive }).ThenInUI((s, d) => output.Add(d))
					   .Finally((s, e) => {
						   success = s;
						   finallyException = e;
					   });

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(finallyException);

			StopTest(watch, logger, taskManager);
		}

		[CustomUnityTest]
		public IEnumerator YouCanUseCatchAtTheEndOfAChain()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var success = false;
			Exception exception = null;
			var output = new List<string>();
			var expectedOutput = new List<string> { "one name" };
			var exceptionMessage = $"{nameof(YouCanUseCatchAtTheEndOfAChain)} an exception";

			var task = new FuncTask<string>(taskManager, _ => "one name") { Affinity = TaskAffinity.UI }
					   .Then((s, d) => output.Add(d)).Then(_ => throw new Exception(exceptionMessage))
					   .Then(new FuncTask<string>(taskManager, _ => "another name") { Affinity = TaskAffinity.Exclusive })
					   .ThenInUI((s, d) => output.Add(d))
					   .Finally((_, __) => {})
					   .Catch(ex => exception = ex);

			// wait for the tasks to finish
			foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

			Assert.IsFalse(success);
			CollectionAssert.AreEqual(expectedOutput, output);
			Assert.IsNotNull(exception);

			StopTest(watch, logger, taskManager);
		}
	}
}
