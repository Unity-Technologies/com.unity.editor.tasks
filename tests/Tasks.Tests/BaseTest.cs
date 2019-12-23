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

		internal TestData StartTest(bool withHttpServer = false,
			[CallerMemberName] string testName = "test") =>
				new TestData(testName, new LogFacade(testName, new NUnitLogAdapter(), TracingEnabled),
					withHttpServer);

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
