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
		public ProcessStartInfo StartInfo { get; }
		public StreamWriter Input { get; protected set; }
		public int ProcessId { get; protected set; }
		public int ExitCode { get; protected set; }
		public bool HasExited { get; protected set; }

		protected BaseProcessWrapper(ProcessStartInfo startInfo)
		{
			startInfo.EnsureNotNull(nameof(startInfo));
			this.StartInfo = startInfo;
		}

		public virtual void Detach() { }

		public abstract void Run();
		public abstract void Stop(bool dontWait = false);

		#region IDisposable Support

		protected virtual void Dispose(bool disposing)
		{ }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}

	public class ProcessWrapper : BaseProcessWrapper
	{
		private readonly List<string> errors = new List<string>();
		private readonly Action onEnd;
		private readonly Action<Exception, string> onError;
		private readonly Action onStart;
		private readonly IOutputProcessor outputProcessor;
		private readonly ManualResetEventSlim stopEvent = new ManualResetEventSlim(false);
		private readonly string taskName;
		private readonly CancellationToken token;

		private ILogging logger;
		private bool detached;

		public ProcessWrapper(string taskName, ProcessStartInfo startInfo,
			IOutputProcessor outputProcessor,
			Action onStart, Action onEnd, Action<Exception, string> onError,
			CancellationToken token)
			: base(startInfo)
		{
			this.taskName = taskName;
			this.outputProcessor = outputProcessor;
			this.onStart = onStart;
			this.onEnd = onEnd;
			this.onError = onError;
			this.token = token;
			Process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
		}

		public override void Detach()
		{
			detached = true;
			stopEvent.Set();
		}

		public override void Run()
		{
			DateTimeOffset lastOutput = DateTimeOffset.UtcNow;
			Exception thrownException = null;
			var gotOutput = new AutoResetEvent(false);

			if (StartInfo.RedirectStandardError)
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

			if (StartInfo.RedirectStandardOutput)
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
				Logger.Trace($"Running '{StartInfo.FileName} {StartInfo.Arguments}' in '{StartInfo.WorkingDirectory}'");

				token.ThrowIfCancellationRequested();

				if (StartInfo.CreateNoWindow)
				{
					Process.Exited += (obj, args) => {
						while (!token.IsCancellationRequested && gotOutput.WaitOne(100))
						{ }
						HasExited = true;
						stopEvent.Set();
					};
				}

				Process.Start();

				ProcessId = Process.Id;

				if (StartInfo.RedirectStandardInput)
					Input = new StreamWriter(Process.StandardInput.BaseStream, new UTF8Encoding(false));
				if (StartInfo.RedirectStandardError)
					Process.BeginErrorReadLine();
				if (StartInfo.RedirectStandardOutput)
					Process.BeginOutputReadLine();

				onStart?.Invoke();

				if (StartInfo.CreateNoWindow)
				{
					stopEvent.Wait(token);
					if (!detached)
					{
						var exited = WaitForExit(500);
						if (exited)
						{
							// process is done and we haven't seen output, we're done
							while (gotOutput.WaitOne(100)) { }
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
				HasExited = true;
				try
				{
					HasExited = Process.HasExited;
				}
				catch { }

				if (!HasExited)
				{
					Stop(true);
				}

				var errorCode = -42;
				if (ex is Win32Exception)
					errorCode = ((Win32Exception)ex).NativeErrorCode;

				StringBuilder sb = new StringBuilder();
				sb.AppendLine($"Error code {errorCode}");
				sb.AppendLine(ex.Message);
				if (StartInfo.Arguments.Contains("-credential"))
					sb.AppendLine($"'{StartInfo.FileName} {taskName}'");
				else
					sb.AppendLine($"'{StartInfo.FileName} {StartInfo.Arguments}'");
				if (errorCode == 2)
					sb.AppendLine("The system cannot find the file specified.");
				sb.AppendLine($"Working directory: {StartInfo.WorkingDirectory}");
				foreach (string env in StartInfo.EnvironmentVariables.Keys)
				{
					sb.AppendFormat("{0}:{1}", env, StartInfo.EnvironmentVariables[env]);
					sb.AppendLine();
				}
				thrownException = new ProcessException(errorCode, sb.ToString(), ex);
			}

			if (!detached)
			{
				try
				{
					if (HasExited)
					{
						ExitCode = Process.ExitCode;
						Process.Close();
					}
				}
				catch
				{ }
			}

			if (thrownException != null || errors.Count > 0)
				onError?.Invoke(thrownException, string.Join(Environment.NewLine, errors.ToArray()));

			onEnd?.Invoke();

			Dispose();
		}

		public override void Stop(bool dontWait = false)
		{
			try
			{
				if (StartInfo.RedirectStandardError)
					Process.CancelErrorRead();
				if (StartInfo.RedirectStandardOutput)
					Process.CancelOutputRead();
				if (!Process.HasExited && StartInfo.RedirectStandardInput)
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
				try
				{
					if (StartInfo.RedirectStandardError)
						Process.CancelErrorRead();
					if (StartInfo.RedirectStandardOutput)
						Process.CancelOutputRead();
				}
				catch { }
				Process.Dispose();
				Input?.Dispose();
				stopEvent.Dispose();
				disposed = true;
			}
		}

		protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
		public Process Process { get; }
}
}
