using System;
using System.Threading.Tasks;
using BaseTests;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace ThreadingTests
{
	// This file is only compiled when building the solution
	// outside of Unity.
	// Unity does not support async/await tests, but it does
	// have a special type of test with a [CustomUnityTest] attribute
	// which mimicks a coroutine in EditMode. This attribute is
	// defined for tests running outside of Unity, and those tests
	// are executed the await RunTest calls below, if running outside
    // of Unity, so I don't have to keep two copies of tests.
	//
	// Tests that Unity can't run or shouldn't run can be added directly
	// here.
	partial class AsyncTests : BaseTest
	{
		[Test]
		public async Task CanWrapATask_()
		{
			await RunTest(CanWrapATask);
		}

		[Test]
		public async Task Inlining_()
		{
			await RunTest(Inlining);
		}

		[Test]
		public async Task StartAsyncWorks()
		{
			using (var test = StartTest())
			{

				var task = test.TaskManager.With(() => 1);

				var waitTask = task.StartAsAsync();
				var retTask = await Task.WhenAny(waitTask, Task.Delay(Timeout));

				Assert.AreEqual(retTask, waitTask);

				var ret = await waitTask;

				Assert.AreEqual(1, ret);

			}
		}

		[Test]
		public async Task StartAwaitSafelyAwaits()
		{
			using (var test = StartTest())
			{

				var task = test.TaskManager.With(() => throw new InvalidOperationException())
				.Catch(_ => { });

				await task.StartAwait(_ => { });

			}
		}
	}
}
