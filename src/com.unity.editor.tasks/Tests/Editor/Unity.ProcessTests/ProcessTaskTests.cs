using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseTests;
using NUnit.Framework;

namespace ProcessManagerTests
{
	using Unity.Editor.Tasks;
	using Unity.Editor.Tasks.Internal.IO;

	public partial class ProcessTaskTests : BaseTest
	{
		[CustomUnityTest]
		public IEnumerator CanRunProcess()
		{
			using (var test = StartTest())
			{

				var task = new FindExecTask(test.TaskManager, test.ProcessManager, test.Environment.IsWindows ? "cmd" : "sh");

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;
			}
		}

		[CustomUnityTest]
		public IEnumerator NestedProcessShouldChainCorrectly()
		{
			using (var test = StartTest())
			{

				var expected = new List<string> { "BeforeProcess", "ok", "AfterProcess", "ProcessFinally", "AfterProcessFinally" };

				var results = new List<string>();

				var beforeProcess = new ActionTask(test.TaskManager, _ => results.Add("BeforeProcess")) { Name = "BeforeProcess" };
				var processTask = new FirstNonNullLineProcessTask(test.TaskManager, test.ProcessManager, TestApp, @"--sleep 1000 --data ""ok""");

				var processOutputTask = new FuncTask<string, int>(test.TaskManager, (b, previous) => {
					results.Add(previous);
					results.Add("AfterProcess");
					return 1234;
				}) { Name = "AfterProcess" };

				var innerChain = processTask.Then(processOutputTask).Finally((b, exception) => results.Add("ProcessFinally"), "ProcessFinally");

				var outerChain = beforeProcess.Then(innerChain).Finally((b, exception) => results.Add("AfterProcessFinally"), "AfterProcessFinally");

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(innerChain, outerChain)) yield return frame;

				results.Matches(expected);

			}
		}

		[CustomUnityTest]
		public IEnumerator MultipleFinallyOrder()
		{
			using (var test = StartTest())
			{
				var results = new List<string>();

				var beforeProcess = new ActionTask(test.TaskManager, _ => results.Add("BeforeProcess")) { Name = "BeforeProcess" };
				var processTask = new FirstNonNullLineProcessTask(test.TaskManager, test.ProcessManager, TestApp, "-x") { Name = "Process" };

				// this will never run because the process throws an exception
				var processOutputTask = new FuncTask<string, int>(test.TaskManager, (success, previous) => {
					results.Add(previous);
					results.Add("ProcessOutput");
					return 1234;
				});

				var innerChain = processTask.Then(processOutputTask).Finally((b, exception) => results.Add("ProcessFinally"), "ProcessFinally");
				var outerChain = beforeProcess.Then(innerChain).Finally((b, exception) => results.Add("AfterProcessFinally"), "AfterProcessFinally");

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(innerChain, outerChain)) yield return frame;

				results.MatchesUnsorted(new[] { "BeforeProcess", "ProcessFinally", "AfterProcessFinally" });
				Assert.AreEqual("BeforeProcess", results[0]);

				var expected = new List<string> { "ProcessFinally", "AfterProcessFinally" };
				results.Skip(1).MatchesUnsorted(expected);

			}
		}

		[CustomUnityTest]
		public IEnumerator ProcessOnStartOnEndTaskOrder()
		{
			using (var test = StartTest())
			{

				var values = new List<string>();
				string process1Value = null;
				string process2Value = null;

				var process1Task = new FirstNonNullLineProcessTask(test.TaskManager, test.ProcessManager, TestApp, @"--sleep 100 -d process1")
								   .Configure(test.ProcessManager)
								   .Then(s => {
									   process1Value = s;
									   values.Add(s);
								   });

				var process2Task = new FirstNonNullLineProcessTask(test.TaskManager, test.ProcessManager, TestApp, @"---sleep 100 -d process2")
								   .Configure(test.ProcessManager)
								   .Then(s => {
									   process2Value = s;
									   values.Add(s);
								   });

				var combinedTask = process1Task.Then(process2Task);

				combinedTask.OnStart += task => { values.Add("OnStart"); };

				combinedTask.OnEnd += (task, success, ex) => { values.Add("OnEnd"); };

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(combinedTask)) yield return frame;

				Assert.AreEqual(process1Value, "process1");
				Assert.AreEqual(process2Value, "process2");
				Assert.True(values.SequenceEqual(new[] { "process1", "OnStart", "process2", "OnEnd" }));

			}
		}

		[CustomUnityTest]
		public IEnumerator ProcessReadsFromStandardInput()
		{
			using (var test = StartTest())
			{

				var input = new List<string> { "Hello", "World\u001A" };

				var expectedOutput = "Hello";

				var procTask = new FirstNonNullLineProcessTask(test.TaskManager, test.ProcessManager, TestApp, @"--sleep 100 -i")
					.Configure(test.ProcessManager);

				procTask.OnStartProcess += proc => {
					foreach (var item in input)
					{
						proc.StandardInput.WriteLine(item);
					}
					proc.StandardInput.Close();
				};

				var chain = procTask.Finally((s, e, d) => d);

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(chain)) yield return frame;

				var output = chain.Result;

				Assert.AreEqual(expectedOutput, output);

			}
		}

		[CustomUnityTest]
		public IEnumerator ProcessReturningErrorThrowsException()
		{
			using (var test = StartTest())
			{

				var success = false;
				Exception thrown1, thrown2;
				thrown1 = thrown2 = null;
				var output = new List<string>();
				var expectedOutput = new[] { "first" };

				// run a process that prints "one name" in the console
				// then run another process that throws an exception and sets exit code to -1
				// exit codes != 0 cause a ProcessException to be thrown
				var task = new FirstNonNullLineProcessTask(test.TaskManager, test.ProcessManager, TestApp, @"--sleep 100 -d ""first""")
						   // this won't run because there's no exception here
						   .Catch(ex => thrown1 = ex)
						   // save the output of the process
						   .Then(output.Add)
						   // run the second process, which is going to throw an exception
						   .Then(new FirstNonNullLineProcessTask(test.TaskManager, test.ProcessManager, TestApp, @"-d ""second"" -e kaboom -r -1"))
						   // this records the exception
						   .Catch(ex => thrown2 = ex)
						   // this never runs because of the exception
						   .Then(output.Add)
						   // this will always run
						   .Finally((s, e) => success = s);

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				Assert.IsFalse(success);
				CollectionAssert.AreEqual(expectedOutput, output);
				Assert.IsNull(thrown1);
				Assert.IsInstanceOf<ProcessException>(thrown2);
				Assert.AreEqual("kaboom", thrown2.Message);

			}
		}

		[CustomUnityTest]
		public IEnumerator CanRunProcessOnMono()
		{
			using (var test = StartTest())
			{
				var monoPath = test.Environment.UnityApplicationContents.ToSPath().Combine("MonoBleedingEdge", "bin", "mono" + test.Environment.ExecutableExtension);
				if (!monoPath.FileExists())
				{
					// we don't have a mono to test with, skip this test
					test.Logger.Info("Skipping test, we don't have mono.");
				}
				else
				{
					var task = new MonoProcessTask(test.TaskManager, test.ProcessManager, TestApp, "-d 1");
					Assert.AreEqual(monoPath, task.ProcessName.ToSPath());
					Assert.AreEqual($"{TestApp} -d 1", task.ProcessArguments);
					foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;
					Assert.AreEqual($"1{test.Environment.NewLine}", task.Result);
				}
			}
		}
	}
}
