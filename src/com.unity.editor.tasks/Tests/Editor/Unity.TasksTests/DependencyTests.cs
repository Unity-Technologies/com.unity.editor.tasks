using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseTests;
using Unity.Editor.Tasks.Extensions;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace ThreadingTests
{
	partial class DependencyTests : BaseTest
	{
		[CustomUnityTest]
		public IEnumerator RunningDifferentTasksDependingOnSuccessOrFailure()
		{
			using (var test = StartTest())
			{

				var callOrder = new List<string>();

				ITask taskStart = new FuncTask<bool>(test.TaskManager, _ => {
					Console.WriteLine("task start");
					callOrder.Add("chain start");
					return false;
				}) { Name = "Chain Start" };

				// this runs because we're forcing a failure up the chain
				ITask taskOnFailure = new ActionTask(test.TaskManager, () => {
					Console.WriteLine("task failure");
					callOrder.Add("on failure");
				}) { Name = "On Failure" };

				// if success - this never runs because we're forcing a failure before it
				ITask taskOnSuccess = new ActionTask(test.TaskManager, () => {
					Console.WriteLine("task success");
					callOrder.Add("on success");
				}) { Name = "On Success" };

				// this will always run because we're adding it after explicit fail/success tasks
				var taskEnd = new ActionTask(test.TaskManager, () => {
					Console.WriteLine("task completed");
					callOrder.Add("chain completed");
				}) { Name = "Chain Completed" };

				// start + forced failure
				taskStart = taskStart
					.Then(new ActionTask<bool>(test.TaskManager, (_, __) => {
						Console.WriteLine("task throw");
						callOrder.Add("failing");
						throw new InvalidOperationException();
					}) { Name = "Failing" });

				// add the failure chain
				taskStart
					.Then(taskOnFailure, runOptions: TaskRunOptions.OnFailure)
					.Then(taskEnd, taskIsTopOfChain: true);

				// add the success chain
				taskStart
					.Then(taskOnSuccess, runOptions: TaskRunOptions.OnSuccess)
					.Then(taskEnd, taskIsTopOfChain: true);

				// taskEnd is added to both chains but only runs once
				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(taskEnd)) yield return frame;

				Assert.AreEqual(new string[] { "chain start", "failing", "on failure", "chain completed" }.Join(","), callOrder.Join(","));

			}
		}

		[CustomUnityTest]
		public IEnumerator TaskOnFailureGetsCalledWhenExceptionHappensUpTheChain()
		{
			using (var test = StartTest())
			{

				var runOrder = new List<string>();
				var exceptions = new List<Exception>();
				var task = new ActionTask(test.TaskManager, _ => throw new InvalidOperationException())
						   .Then(() => runOrder.Add("1"))
						   .Catch(ex => exceptions.Add(ex))
						   .Then(() => runOrder.Add("OnFailure"),
							   runOptions: TaskRunOptions.OnFailure)
						   .Finally((s, e) => { });

				// wait for the tasks to finish
				foreach (var frame in StartAndWaitForCompletion(task)) yield return frame;

				CollectionAssert.AreEqual(new string[] { typeof(InvalidOperationException).Name }, exceptions.Select(x => x.GetType().Name).ToArray());
				CollectionAssert.AreEqual(new string[] { "OnFailure" }, runOrder);

			}
		}
	}
}
