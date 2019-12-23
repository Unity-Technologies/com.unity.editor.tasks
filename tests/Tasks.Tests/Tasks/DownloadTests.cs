namespace DownloadTests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using BaseTests;
	using NUnit.Framework;
	using Unity.Editor.Tasks;
	using Unity.Editor.Tasks.Helpers;
	using Unity.Editor.Tasks.Extensions;
	using Unity.Editor.Tasks.Internal.IO;

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

	partial class DownloadTaskTests : BaseTest
	{
		[Test]
		public void ShutdownTimeWhenTaskManagerDisposed()
		{
			using (var test = StartTest(withHttpServer: true))
			{
				test.HttpServer.Delay = 100;

				var fileSystem = SPath.FileSystem;

				var evtStop = new AutoResetEvent(false);
				var evtFinally = new AutoResetEvent(false);
				Exception exception = null;

				var gitLfs = new UriString($"http://localhost:{test.HttpServer.Port}/unity/git/windows/git-lfs.zip");

				var downloadGitTask = new DownloadTask(test.TaskManager, gitLfs, test.TestPath)

									  // An exception is thrown when we stop the task manager
									  // since we're stopping the task manager, no other tasks
									  // will run, which means we can only hook with Catch
									  // or with the Finally overload that runs on the same thread (not as a task)
									  .Catch(e => {
										  exception = e;
										  evtFinally.Set();
									  })
									  .Progress(p => { evtStop.Set(); });

				downloadGitTask.Start();

				Assert.True(evtStop.WaitOne(Timeout));

				test.TaskManager.Dispose();

				Assert.True(evtFinally.WaitOne(Timeout));

				test.HttpServer.Delay = 0;
				test.HttpServer.Abort();

				Assert.NotNull(exception);
			}
		}

		[Test]
		public async Task ResumingDownloadsWorks()
		{
			using (var test = StartTest(withHttpServer: true))
			{
				var url = new UriString($"http://localhost:{test.HttpServer.Port}/misc/file.zip");
				var downloadTask = new DownloadTask(test.TaskManager, url, test.TestPath);

				var task = await Task.WhenAny(downloadTask.Start().Task, Task.Delay(Timeout));

				// test if it timed out
				Assert.AreEqual(downloadTask.Task, task);

				var downloadPath = (await downloadTask.Task).ToSPath();
				Assert.NotNull(downloadPath);

				var expected = "2084ced87ebba89ffc4796ef47a4dd11";
				Assert.AreEqual(expected, downloadPath.ToMD5());

				var downloadPathBytes = downloadPath.ReadAllBytes();

				var cutDownloadPathBytes = downloadPathBytes.Take(downloadPathBytes.Length - 1000).ToArray();

				downloadPath.Delete();

				new SPath(downloadPath + ".partial").WriteAllBytes(cutDownloadPathBytes);

				downloadTask = new DownloadTask(test.TaskManager, url, test.TestPath);

				task = await Task.WhenAny(downloadTask.Start().Task, Task.Delay(Timeout));

				// test if it timed out
				Assert.AreEqual(downloadTask.Task, task);

				downloadPath = (await downloadTask.Task).ToSPath();

				Assert.AreEqual(expected, downloadPath.ToMD5());
			}
		}

	}

	partial class DownloaderTests : BaseTest
	{
		//[Test]
		public async Task DownloadsRunSideBySide()
		{
			using (var test = StartTest(withHttpServer: true))
			{
				// simulate a slow connection
				test.HttpServer.Delay = 50;

				var url1 = new UriString($"http://localhost:{test.HttpServer.Port}/misc/anotherfile.zip");
				var url2 = new UriString($"http://localhost:{test.HttpServer.Port}/misc/anotherfile2.zip");

				var events = new Dictionary<string, long[]>();

				var downloader = new Downloader(test.TaskManager);
				var watch = test.Watch;

				downloader.QueueDownload(url1, test.TestPath);
				downloader.QueueDownload(url2, test.TestPath);
				downloader.OnDownloadStart += url => events.Add(url.Filename, new [] { watch.ElapsedMilliseconds, 0 });
				downloader.OnDownloadComplete += (url, file) => events[url.Filename][1] = watch.ElapsedMilliseconds;

				var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));

				Assert.AreEqual(downloader.Task, task);

				Assert.AreEqual(2, events.Count);

				// did both downloads start at the same time, i.e. less than 50ms apart (the set delay on the http server)
				Assert.True(Math.Abs(events[url1.Filename][0] - events[url1.Filename][0]) < test.HttpServer.Delay);
			}
		}

		[Test]
		public async Task SucceedIfEverythingIsAlreadyDownloaded()
		{
			using (var test = StartTest(withHttpServer: true))
			{
				var fileSystem = SPath.FileSystem;
				var url1 = new UriString($"http://localhost:{test.HttpServer.Port}/misc/file.zip");

				var downloader = new Downloader(test.TaskManager);

				downloader.QueueDownload(url1, test.TestPath);

				var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));

				Assert.AreEqual(downloader.Task, task);
				var downloadData = await downloader.Task;
				var downloadPath = downloadData.FirstOrDefault().File.ToSPath();

				downloader = new Downloader(test.TaskManager);

				downloader.QueueDownload(url1, test.TestPath);
				task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));

				Assert.AreEqual(downloader.Task, task);
				downloadData = await downloader.Task;
				downloadPath = downloadData.FirstOrDefault().File.ToSPath();

				var expected = "2084ced87ebba89ffc4796ef47a4dd11";
				Assert.AreEqual(expected, downloadPath.ToMD5());
			}
		}

		[Test]
		public async Task DownloadingNonExistingFileThrows()
		{
			using (var test = StartTest(withHttpServer: true))
			{
				var url = $"http://localhost:{test.HttpServer.Port}/nope";

				var downloader = new Downloader(test.TaskManager);
				downloader.QueueDownload(url, test.TestPath);

				var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));
				Assert.AreEqual(downloader.Task, task);
				Assert.ThrowsAsync<DownloadException>(async () => await downloader.Task);
			}
		}

		[Test]
		public void DownloadingFromNonExistingDomainThrows()
		{
			using (var test = StartTest(withHttpServer: true))
			{

				var fileSystem = SPath.FileSystem;

				var downloadTask = new DownloadTask(test.TaskManager, "http://ggggithub.com/robots.txt", test.TestPath);
				var exceptionThrown = false;

				var autoResetEvent = new AutoResetEvent(false);

				downloadTask.FinallyInline(success => {
					            exceptionThrown = !success;
					            autoResetEvent.Set();
				            })
				            .Start();

				Assert.True(autoResetEvent.WaitOne(Timeout));
				Assert.True(exceptionThrown);
			}
		}


	}
}
