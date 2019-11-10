using System;
using System.Threading.Tasks;
using BaseTests;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace ThreadingTests
{
	partial class AsyncTests : BaseTest
	{
		[Test]
		public async Task StartAsyncWorks()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var task = new FuncTask<int>(taskManager, _ => 1);

			var waitTask = task.StartAsAsync();
			var retTask = await Task.WhenAny(waitTask, Task.Delay(Timeout));

			Assert.AreEqual(retTask, waitTask);

			var ret = await waitTask;

			Assert.AreEqual(1, ret);

			StopTest(watch, logger, taskManager);
		}

		[Test]
		public async Task StartAwaitSafelyAwaits()
		{
			StartTest(out var watch, out var logger, out var taskManager);

			var task = new ActionTask(taskManager, _ => throw new InvalidOperationException())
				.Catch(_ => { });

			await task.StartAwait(_ => { });

			StopTest(watch, logger, taskManager);
		}
	}
}
