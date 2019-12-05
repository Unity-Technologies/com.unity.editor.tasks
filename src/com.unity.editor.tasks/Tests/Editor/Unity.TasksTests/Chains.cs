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
			using (var test = StartTest())
			{

				var success = false;
				Exception exception = null;
				Exception finallyException = null;
				var runOrder = new List<string>();
				var output = new List<string>();
				var expectedOutput = new List<string> { "one name" };
				var exceptionMessage = $"{nameof(CatchAlwaysRunsBeforeFinally)} an exception";

				var task = test.TaskManager.WithUI(() => "one name", $"{nameof(CatchAlwaysRunsBeforeFinally)} Task 1")
						   .Then(output.Add, $"{nameof(CatchAlwaysRunsBeforeFinally)} Task 2")
						   .Then(() => throw new Exception(exceptionMessage))
						   .ThenInExclusive(() => "another name", $"{nameof(CatchAlwaysRunsBeforeFinally)} Task 3")
						   .Then(d => {
							   output.Add(d);
							   return "done";
						   }, $"{nameof(CatchAlwaysRunsBeforeFinally)} Task 4")
						   .Catch(ex => {
							   lock (runOrder)
							   {
								   exception = ex;
								   runOrder.Add("catch");
							   }
						   })
						   .Finally((s, e, d) => {
							   lock (runOrder)
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

			}
		}

		[CustomUnityTest]
		public IEnumerator FinallyCanAlsoNotReturnData()
		{
			using (var test = StartTest())
			{

				var success = false;
				Exception exception = null;
				Exception finallyException = null;
				var runOrder = new List<string>();
				var output = new List<string>();
				var expectedOutput = new List<string> { "one name", "another name", "done" };

				var task = test.TaskManager.With(() => "one name")
						   .Then(output.Add)
						   .ThenInExclusive(() => "another name")
						   .Then(d => {
							   output.Add(d);
							   return "done";
						   }).Finally((s, e, d) => {
							   lock (runOrder)
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

			}
		}

		[CustomUnityTest]
		public IEnumerator FinallyCanReturnData()
		{
			using (var test = StartTest())
			{

				var success = false;
				Exception exception = null;
				Exception finallyException = null;
				var runOrder = new List<string>();
				var output = new List<string>();
				var expectedOutput = new List<string> { "one name", "another name", "done" };

				var task = test.TaskManager.WithUI(() => "one name")
						   .Then(output.Add)
						   .ThenInExclusive(() => "another name")
						   .Then(d => {
							   output.Add(d);
							   return "done";
						   }).Catch(ex => {
							   lock (runOrder)
							   {
								   exception = ex;
								   runOrder.Add("catch");
							   }
						   }).Finally((s, e, d) => {
							   lock (runOrder)
							   {
								   success = s;
								   output.Add(d);
								   finallyException = e;
								   runOrder.Add("finally");
							   }
							   return d;
						   })
						   .ThenInUI(d => {
							   lock (runOrder)
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

			}
		}

		[CustomUnityTest]
		public IEnumerator FinallyReportsException()
		{
			using (var test = StartTest())
			{

				var success = false;
				Exception finallyException = null;
				var output = new List<string>();
				var expectedOutput = new List<string> { "one name" };
				var lck = new object();
				var count = 0;
				var exceptionMessage = $"{nameof(FinallyReportsException)} an exception";

				var task = test.TaskManager.WithUI(() => "one name", "Task1")
						   .Then(output.Add, "Task2")
						   .Then(() => throw new Exception(exceptionMessage), "Throwing")
						   .ThenInExclusive(() => "another name", "Task3")
						   .ThenInUI(output.Add, "Task4")
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

				lock (lck)
				{
					Assert.AreEqual(3, count);
				}

				Assert.IsFalse(success);
				CollectionAssert.AreEqual(expectedOutput, output);
				Assert.IsNotNull(finallyException);
				Assert.AreEqual(exceptionMessage, finallyException.Message);

			}
		}

		[CustomUnityTest]
		public IEnumerator ThrowingInterruptsTaskChainButAlwaysRunsFinallyAndCatch()
		{
			using (var test = StartTest())
			{
				var success = false;
				string thrown = "";
				Exception finallyException = null;
				var output = new List<string>();
				var expectedOutput = new List<string> { "one name" };

				var task = test.TaskManager.WithUI(() => "one name")
						   .Then(output.Add)
						   .Then(() => throw new Exception("an exception")).Catch(ex => thrown = ex.Message)
						   .ThenInExclusive(() => "another name")
						   .ThenInUI(output.Add)
						   .Finally((s, e) => {
							   success = s;
							   finallyException = e;
						   });

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				Assert.IsFalse(success);
				CollectionAssert.AreEqual(expectedOutput, output);
				Assert.IsNotNull(finallyException);

			}
		}

		[CustomUnityTest]
		public IEnumerator YouCanUseCatchAtTheEndOfAChain()
		{
			using (var test = StartTest())
			{

				var success = false;
				Exception exception = null;
				var output = new List<string>();
				var expectedOutput = new List<string> { "one name" };
				var exceptionMessage = $"{nameof(YouCanUseCatchAtTheEndOfAChain)} an exception";

				var task = test.TaskManager.WithUI(() => "one name")
							.Then(output.Add)
							.Then(() => throw new Exception(exceptionMessage))
							.ThenInExclusive(() => "another name")
							.ThenInUI(d => output.Add(d))
							.Finally((_, __) => { })
							.Catch(ex => exception = ex);

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				Assert.IsFalse(success);
				CollectionAssert.AreEqual(expectedOutput, output);
				Assert.IsNotNull(exception);

			}
		}
	}
}
