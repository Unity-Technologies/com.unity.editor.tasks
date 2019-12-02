namespace Unity.Editor.Tasks
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using System.Threading;
	using Extensions;
	using Helpers;
	using Logging;

	public abstract class BaseProcessWrapper : IDisposable
	{
		public Process Process { get; }
		public StreamWriter Input { get; protected set; }
		public int ProcessId { get; protected set; }
		public int ExitCode { get; protected set; }

		protected BaseProcessWrapper(Process process)
		{
			process.EnsureNotNull(nameof(process));
			this.Process = process;
		}

		public virtual void Detach() {}

		public abstract void Run();
		public abstract void Stop(bool dontWait = false);

		#region IDisposable Support

		protected virtual void Dispose(bool disposing)
		{}

		public void Dispose()
		{
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}

	class ProcessWrapper : BaseProcessWrapper, IDisposable
	{
		private readonly List<string> errors = new List<string>();
		private readonly bool longRunning;
		private readonly RaiseAndDiscardOutputProcessor longRunningOutputProcessor;
		private readonly Action onEnd;
		private readonly Action<Exception, string> onError;
		private readonly Action onStart;
		private readonly IOutputProcessor outputProcessor;
		private readonly ManualResetEventSlim stopEvent = new ManualResetEventSlim(false);
		private readonly string taskName;
		private readonly CancellationToken token;

		private ILogging logger;
		private bool released;

		public ProcessWrapper(string taskName, Process process,
			IOutputProcessor outputProcessor,
			bool longRunning,
			Action onStart, Action onEnd, Action<Exception, string> onError,
			CancellationToken token)
			: base(process)
		{
			this.taskName = taskName;
			this.outputProcessor = outputProcessor;
			this.onStart = onStart;
			this.onEnd = onEnd;
			this.onError = onError;
			this.token = token;
			this.longRunning = longRunning;

			if (this.longRunning)
				longRunningOutputProcessor = new RaiseAndDiscardOutputProcessor();
		}

		public override void Detach()
		{
			released = true;
			stopEvent.Set();
		}

		public override void Run()
		{
			DateTimeOffset lastOutput = DateTimeOffset.UtcNow;
			Exception thrownException = null;
			var gotOutput = new AutoResetEvent(false);
			var startInfo = Process.StartInfo;

			if (startInfo.RedirectStandardError)
			{
				Process.ErrorDataReceived += (s, e) => {
					//if (e.Data != null)
					//{
					//    Logger.Trace("ErrorData \"" + (e.Data == null ? "'null'" : e.Data) + "\"");
					//}

					lastOutput = DateTimeOffset.UtcNow;
					gotOutput.Set();
					if (e.Data != null)
					{
						var line = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(e.Data));
						errors.Add(line.TrimEnd('\r', '\n'));
					}
				};
			}

			if (startInfo.RedirectStandardOutput)
			{
				Process.OutputDataReceived += (s, e) => {
					try
					{
						lastOutput = DateTimeOffset.UtcNow;
						gotOutput.Set();
						if (e.Data != null)
						{
							var line = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(e.Data)).TrimEnd('\r', '\n');
							outputProcessor.Process(line);
						}
						else
						{
							outputProcessor.Process(null);
						}
					}
					catch (Exception ex)
					{
						errors.Add(ex.GetExceptionMessageShort());
					}
				};
			}

			try
			{
				Logger.Trace($"Running '{startInfo.FileName} {startInfo.Arguments}' in '{startInfo.WorkingDirectory}'");

				token.ThrowIfCancellationRequested();

				if (startInfo.CreateNoWindow)
				{
					Process.Exited += (obj, args) => {
						while (!token.IsCancellationRequested && gotOutput.WaitOne(100))
						{}
						stopEvent.Set();
					};
				}

				Process.Start();

				ProcessId = Process.Id;

				if (startInfo.RedirectStandardInput)
					Input = new StreamWriter(Process.StandardInput.BaseStream, new UTF8Encoding(false));
				if (startInfo.RedirectStandardError)
					Process.BeginErrorReadLine();
				if (startInfo.RedirectStandardOutput)
					Process.BeginOutputReadLine();

				onStart?.Invoke();

				if (startInfo.CreateNoWindow)
				{
					stopEvent.Wait(token);
					if (!released)
					{
						var exited = WaitForExit(500);
						if (exited)
						{
							// process is done and we haven't seen output, we're done
							while (gotOutput.WaitOne(100)) {}
						}
						else if (token.IsCancellationRequested)
							// if we're exiting
						{
							Stop(true);
							ExitCode = Process.ExitCode;
							token.ThrowIfCancellationRequested();
							throw new ProcessException(-2, "Process timed out");
						}

						if (Process.ExitCode != 0 && errors.Count > 0)
						{
							thrownException = new ProcessException(Process.ExitCode, string.Join(Environment.NewLine, errors.ToArray()));
						}
					}
				}
			}
			catch (Exception ex)
			{
				bool hasExited = true;
				try
				{
					hasExited = Process.HasExited;
				} catch {}

				if (!hasExited)
				{
					Stop(true);
				}

				var errorCode = -42;
				if (ex is Win32Exception)
					errorCode = ((Win32Exception)ex).NativeErrorCode;

				StringBuilder sb = new StringBuilder();
				sb.AppendLine($"Error code {errorCode}");
				sb.AppendLine(ex.Message);
				if (startInfo.Arguments.Contains("-credential"))
					sb.AppendLine($"'{startInfo.FileName} {taskName}'");
				else
					sb.AppendLine($"'{startInfo.FileName} {startInfo.Arguments}'");
				if (errorCode == 2)
					sb.AppendLine("The system cannot find the file specified.");
				sb.AppendLine($"Working directory: {startInfo.WorkingDirectory}");
				foreach (string env in startInfo.EnvironmentVariables.Keys)
				{
					sb.AppendFormat("{0}:{1}", env, startInfo.EnvironmentVariables[env]);
					sb.AppendLine();
				}
				thrownException = new ProcessException(errorCode, sb.ToString(), ex);
			}

			if (!released)
			{
				try
				{
					ExitCode = Process.ExitCode;
					Process.Close();
				}
				catch
				{}
			}

			if (thrownException != null || errors.Count > 0)
				onError?.Invoke(thrownException, string.Join(Environment.NewLine, errors.ToArray()));

			onEnd?.Invoke();
		}

		public void StartCommandToLongRunningProcess(IEnumerable<string> input, IOutputProcessor outputProcessor)
		{
			longRunningOutputProcessor.OnEntry += outputProcessor.Process;
			foreach (var line in input)
				Process.StandardInput.WriteLine(line);
		}

		public void FinishCommandToLongRunningProcess(IOutputProcessor outputProcessor)
		{
			longRunningOutputProcessor.OnEntry -= outputProcessor.Process;
		}

		public override void Stop(bool dontWait = false)
		{
			try
			{
				if (Process.StartInfo.RedirectStandardError)
					Process.CancelErrorRead();
				if (Process.StartInfo.RedirectStandardOutput)
					Process.CancelOutputRead();
				if (!Process.HasExited && Process.StartInfo.RedirectStandardInput)
					Input.WriteLine("\x3");
				if (Process.HasExited)
					return;
			}
			catch
			{ }

			try
			{
				bool waitSucceeded = false;
				if (!dontWait)
				{
					waitSucceeded = Process.WaitForExit(500);
				}

				if (!waitSucceeded)
				{
					Process.Kill();
					waitSucceeded = Process.WaitForExit(100);
				}
			}
			catch (Exception ex)
			{
				Logger.Trace(ex);
			}
			stopEvent.Set();
		}

		private bool WaitForExit(int milliseconds)
		{
			//Logger.Debug("WaitForExit - time: {0}ms", milliseconds);

			// Workaround for a bug in which some data may still be processed AFTER this method returns true, thus losing the data.
			// http://connect.microsoft.com/VisualStudio/feedback/details/272125/waitforexit-and-waitforexit-int32-provide-different-and-undocumented-implementations
			bool waitSucceeded = Process.WaitForExit(milliseconds);
			if (waitSucceeded)
			{
				Process.WaitForExit();
			}
			return waitSucceeded;
		}

		private bool disposed;

		protected override void Dispose(bool disposing)
		{
			if (disposed) return;
			if (disposing)
			{
				Process?.Dispose();
				Input?.Dispose();
				stopEvent.Dispose();
				disposed = true;
			}
		}

		protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
	}
}
