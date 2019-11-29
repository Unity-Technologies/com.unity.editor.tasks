using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace BaseTests
{
	using System;

	// Unity does not support async/await tests, but it does
	// have a special type of test with a [CustomUnityTest] attribute
	// which mimicks a coroutine in EditMode. This attribute is
	// defined here so the tests can be compiled without
	// referencing Unity, and nunit on the command line
	// outside of Unity can execute the tests. Basically I don't
	// want to keep two copies of all the tests.
	public class CustomUnityTestAttribute : UnityTestAttribute
	{ }


	public partial class BaseTest : IDisposable
	{
		private LogAdapterBase existingLogger;
		private bool existingTracing;

		public BaseTest()
		{
			// set up the logger so it doesn't write exceptions to the unity log, the test runner doesn't like it
			existingLogger = LogHelper.LogAdapter;
			existingTracing = LogHelper.TracingEnabled;
			LogHelper.TracingEnabled = false;
			LogHelper.LogAdapter = new NullLogAdapter();
		}

		public void Dispose()
		{
			LogHelper.LogAdapter = existingLogger;
			LogHelper.TracingEnabled = existingTracing;
		}

		protected void StartTest(out Stopwatch watch, out ILogging logger, out ITaskManager taskManager, [CallerMemberName] string testName = "test")
		{
			taskManager = new TaskManager().Initialize();

			Debug.Log($"Starting test fixture. Main thread is {taskManager.UIThread}");

			logger = new LogFacade(testName, new UnityLogAdapter(), true);
			watch = new Stopwatch();

			logger.Trace("START");
			watch.Start();
		}

		protected void StartTest(out System.Diagnostics.Stopwatch watch, out ILogging logger, out ITaskManager taskManager,
			out string testPath, out IEnvironment environment, out IProcessManager processManager,
			[CallerMemberName] string testName = "test")
		{
			logger = new LogFacade(testName, new UnityLogAdapter(), true);
			watch = new System.Diagnostics.Stopwatch();

			taskManager = TaskManager;

			testPath = SPath.CreateTempDirectory(testName);

			environment = new UnityEnvironment(testName);
			((UnityEnvironment)environment).SetWorkingDirectory(testPath);
			environment.Initialize(testPath, testPath, "2018.4", testPath, testPath.Combine("Assets"));

			processManager = new ProcessManager(environment, taskManager.Token);

			logger.Trace("START");
			watch.Start();
		}

        protected void StopTest(Stopwatch watch, ILogging logger, ITaskManager taskManager)
		{
			watch.Stop();
			logger.Trace($"END:{watch.ElapsedMilliseconds}ms");
			taskManager.Dispose();
		}

		protected SPath? testApp;

		protected SPath TestApp
		{
			get
			{
				if (!testApp.HasValue)
				{
					testApp = System.IO.Path.GetFullPath("Packages/com.unity.editor.tasks/Tests/Helpers~/Helper.CommandLine.exe");
					if (!testApp.Value.FileExists())
					{
						UnityEngine.Debug.LogException(new InvalidOperationException("Test helper binaries are missing. Build the UnityTools.sln solution once with `dotnet build` in order to set up the tests."));
					}
				}
				return testApp.Value;
			}
		}
    }
}
