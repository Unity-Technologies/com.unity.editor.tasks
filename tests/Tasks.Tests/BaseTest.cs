using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Threading;
using Unity.Editor.Tasks.Logging;
using Unity.Editor.Tasks;
using System.Threading.Tasks;

namespace BaseTests
{
	public partial class BaseTest
	{
		public const bool TracingEnabled = false;

		public BaseTest()
		{
			LogHelper.LogAdapter = new NUnitLogAdapter();
			LogHelper.TracingEnabled = TracingEnabled;
		}

		protected void StartTest(out Stopwatch watch, out ILogging logger, out ITaskManager taskManager, [CallerMemberName] string testName = "test")
		{
			taskManager = new TaskManager();
			try
			{
				taskManager.Initialize();
			}
			catch
			{
				// we're on the nunit sync context, which can't be used to create a task scheduler
				// so use a different context as the main thread. The test won't run on the main nunit thread
				taskManager.Initialize(new TestThreadSynchronizationContext(default));
			}

			LogHelper.Trace($"Starting test. Main thread is {taskManager.UIThread}");

			logger = new LogFacade(testName, new NUnitLogAdapter(), TracingEnabled);
			watch = new Stopwatch();

			logger.Trace("START");
			watch.Start();
		}

		protected void StopTest(Stopwatch watch, ILogging logger, ITaskManager taskManager)
		{
			watch.Stop();
			logger.Trace($"END:{watch.ElapsedMilliseconds}ms");
			taskManager.Dispose();
		}

		protected async Task RunTest(Func<IEnumerator> testMethodToRun)
		{
			var scheduler = ThreadingHelper.GetUIScheduler(new TestThreadSynchronizationContext(default));
			var taskStart = new Task<IEnumerator>(testMethodToRun);
			taskStart.Start(scheduler);
			var e = await RunOn(testMethodToRun, scheduler);
			while (await RunOn(s => ((IEnumerator)s).MoveNext(), e, scheduler))
			{ }
		}

		private Task<T> RunOn<T>(Func<T> method, TaskScheduler scheduler)
		{
			return Task<T>.Factory.StartNew(method, CancellationToken.None, TaskCreationOptions.None, scheduler);
		}

		private Task<T> RunOn<T>(Func<object, T> method, object state, TaskScheduler scheduler)
		{
			return Task<T>.Factory.StartNew(method, state, CancellationToken.None, TaskCreationOptions.None, scheduler);
		}
	}
}
