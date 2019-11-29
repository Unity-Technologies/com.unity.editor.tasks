using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Threading;
using Unity.Editor.Tasks.Logging;
using Unity.Editor.Tasks;
using System.Threading.Tasks;
using SpoiledCat.SimpleIO;

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
			SetupTest(out watch, out logger, out taskManager, testName);

			logger.Trace("START");
			watch.Start();
		}

		private static void SetupTest(out Stopwatch watch, out ILogging logger, out ITaskManager taskManager, String testName)
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
				taskManager.Initialize(new MainThreadSynchronizationContext(taskManager.Token));
			}

			logger = new LogFacade(testName, new NUnitLogAdapter(), TracingEnabled);
			watch = new Stopwatch();
		}

		protected void StartTest(out Stopwatch watch, out ILogging logger, out ITaskManager taskManager,
			out SPath testPath, out IEnvironment environment, out IProcessManager processManager,
			[CallerMemberName] string testName = "test")
		{
			SetupTest(out watch, out logger, out taskManager, testName);

			testPath = SPath.CreateTempDirectory(testName);

			environment = new UnityEnvironment(testName).Initialize(testPath.ToString(SlashMode.Forward), testPath.ToString(SlashMode.Forward), testPath.ToString(SlashMode.Forward));
			processManager = new ProcessManager(environment);

			logger.Trace($"START {testName}");
			watch.Start();
		}

		protected void StopTest(Stopwatch watch, ILogging logger, ITaskManager taskManager,
			[CallerMemberName] string testName = "test")
		{
			watch.Stop();
			logger.Trace($"STOP {testName} :{watch.ElapsedMilliseconds}ms");
			taskManager.Dispose();
            if (SynchronizationContext.Current is IMainThreadSynchronizationContext ourContext)
                ourContext.Dispose();
		}

		protected void StopTest(Stopwatch watch,
			ILogging logger,
			ITaskManager taskManager,
			SPath testPath,
			IEnvironment environment,
			IProcessManager processManager,
			[CallerMemberName] string testName = "test")
		{
			StopTest(watch, logger, taskManager);
            testPath.DeleteIfExists();
            logger.Trace($"STOP {testName}");
		}

		protected async Task RunTest(Func<IEnumerator> testMethodToRun)
		{
			var scheduler = ThreadingHelper.GetUIScheduler(new ThreadSynchronizationContext(default));
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

		protected SPath? testApp;

		protected SPath TestApp
		{
			get
			{
				if (!testApp.HasValue)
					testApp = System.Reflection.Assembly.GetExecutingAssembly().Location.ToSPath().Parent.Combine("Helper.CommandLine.exe");
				return testApp.Value;
			}
		}

	}
}
