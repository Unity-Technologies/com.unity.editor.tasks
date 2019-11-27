// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Unity.Editor.Tasks
{
	using Logging;
	using Extensions;
	using Unity.Editor.Tasks.Helpers;

	public static class ProcessTaskExtensions
	{
		public static T Configure<T>(this T task, IProcessManager processManager,
			 string workingDirectory = null,
			 bool withInput = false)
			 where T : IProcessTask
		{
			return processManager.Configure(task, workingDirectory, withInput);
		}

		public static void Configure(this ProcessStartInfo psi, IProcessEnvironment processEnvironment, string workingDirectory = null)
		{
			processEnvironment.Configure(psi, workingDirectory);
		}
	}

	public interface IProcessTask : ITask, IProcess
	{
		IProcessEnvironment ProcessEnvironment { get; }
		new IProcessTask Start();
	}

	public interface IProcessTask<T> : ITask<T>, IProcessTask
	{
		void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor);
		new IProcessTask<T> Start();
	}

	public interface IProcessTask<TData, T> : ITask<TData, T>, IProcessTask
	{
		void Configure(ProcessStartInfo psi, IOutputProcessor<TData, T> processor);
		new IProcessTask<TData, T> Start();
	}

	class ProcessWrapper
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

		public ProcessWrapper(string taskName, Process process, IOutputProcessor outputProcessor,
			 Action onStart, Action onEnd, Action<Exception, string> onError,
			 CancellationToken token)
		{
			this.taskName = taskName;
			this.outputProcessor = outputProcessor;
			this.onStart = onStart;
			this.onEnd = onEnd;
			this.onError = onError;
			this.token = token;
			this.Process = process;
		}

		public ProcessWrapper(string taskName, Process process,
			 Action onStart, Action onEnd, Action<Exception, string> onError,
			 CancellationToken token)
		{
			longRunning = true;
			outputProcessor = longRunningOutputProcessor = new RaiseAndDiscardOutputProcessor();

			this.taskName = taskName;
			this.onStart = onStart;
			this.onEnd = onEnd;
			this.onError = onError;
			this.token = token;
			this.Process = process;
		}

		public void Run()
		{
			DateTimeOffset lastOutput = DateTimeOffset.UtcNow;
			Exception thrownException = null;
			var gotOutput = new AutoResetEvent(false);
			if (Process.StartInfo.RedirectStandardError)
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
						Logger.Trace(line);
					}
				};
			}

			if (Process.StartInfo.RedirectStandardOutput)
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
						Logger.Error(ex);
					}
				};
			}

			try
			{
				Logger.Trace($"Running '{Process.StartInfo.FileName} {Process.StartInfo.Arguments}'");

				token.ThrowIfCancellationRequested();

				if (Process.StartInfo.CreateNoWindow && longRunning)
				{
					Process.Exited += (obj, args) => {
						while (!token.IsCancellationRequested && gotOutput.WaitOne(100))
						{}
						stopEvent.Set();
					};
				}

				Process.Start();

				ProcessId = Process.Id;

				if (Process.StartInfo.RedirectStandardInput)
					Input = new StreamWriter(Process.StandardInput.BaseStream, new UTF8Encoding(false));
				if (Process.StartInfo.RedirectStandardError)
					Process.BeginErrorReadLine();
				if (Process.StartInfo.RedirectStandardOutput)
					Process.BeginOutputReadLine();

				onStart?.Invoke();

				if (Process.StartInfo.CreateNoWindow)
				{
					if (longRunning)
					{
						stopEvent.Wait(token);
					}
					else
					{
						bool done = false;
						while (!done)
						{
							var exited = WaitForExit(500);
							if (exited)
							{
								// process is done and we haven't seen output, we're done
								done = !gotOutput.WaitOne(100);
							}
							else if (token.IsCancellationRequested
									/* || (taskName.Contains("git lfs") && lastOutput.AddMilliseconds(ApplicationConfiguration.DefaultGitTimeout) < DateTimeOffset.UtcNow) */
									)
								// if we're exiting or we haven't had output for a while
							{
								Stop(true);
								ExitCode = Process.ExitCode;
								token.ThrowIfCancellationRequested();
								throw new ProcessException(-2, "Process timed out");
							}
						}
					}

					if (Process.ExitCode != 0 && errors.Count > 0)
					{
						thrownException = new ProcessException(Process.ExitCode, string.Join(Environment.NewLine, errors.ToArray()));
					}
				}
			}
			catch (Exception ex)
			{
				if (!Process.HasExited)
				{
					Stop(true);
				}

				var errorCode = -42;
				if (ex is Win32Exception)
					errorCode = ((Win32Exception)ex).NativeErrorCode;

				StringBuilder sb = new StringBuilder();
				sb.AppendLine($"Error code {errorCode}");
				sb.AppendLine(ex.Message);
				if (Process.StartInfo.Arguments.Contains("-credential"))
					sb.AppendLine($"'{Process.StartInfo.FileName} {taskName}'");
				else
					sb.AppendLine($"'{Process.StartInfo.FileName} {Process.StartInfo.Arguments}'");
				if (errorCode == 2)
					sb.AppendLine("The system cannot find the file specified.");
				sb.AppendLine($"Working directory: {Process.StartInfo.WorkingDirectory}");
				foreach (string env in Process.StartInfo.EnvironmentVariables.Keys)
				{
					sb.AppendFormat("{0}:{1}", env, Process.StartInfo.EnvironmentVariables[env]);
					sb.AppendLine();
				}
				thrownException = new ProcessException(errorCode, sb.ToString(), ex);
			}

			ExitCode = Process.ExitCode;

			try
			{
				Process.Close();
			} catch {}

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

		public void Stop(bool dontWait = false)
		{
			try
			{
				if (Process.StartInfo.RedirectStandardError)
					Process.CancelErrorRead();
				if (Process.StartInfo.RedirectStandardOutput)
					Process.CancelOutputRead();
				if (!Process.HasExited && Process.StartInfo.RedirectStandardInput)
					Input.WriteLine("\x3");
			}
			catch
			{ }

			if (Process.HasExited)
				return;

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

		public Process Process { get; }
		public StreamWriter Input { get; private set; }
		public int ProcessId { get; private set; }
		public int ExitCode { get; private set; }
		protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
	}

	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T">The type of the results. If it's a List<> or similar, then specify the full List<> type here and the inner type of the List in <typeparam name="TData"/>
	/// <typeparam name="TData">If <typeparam name="TData"/> is a list or similar, then specify its inner type here</typeparam>
	public class ProcessTask<T> : TaskBase<T>, IProcessTask<T>
	{
		private IOutputProcessor<T> outputProcessor;

		private Exception thrownException = null;
		private ProcessWrapper wrapper;
		public event Action<IProcess> OnEndProcess;

		public event Action<string> OnErrorData;
		public event Action<IProcess> OnStartProcess;

		protected ProcessTask() {}

		/// <summary>
		/// Runs a Process with the passed arguments
		/// </summary>
		/// <param name="executable"></param>
		/// <param name="arguments"></param>
		/// <param name="outputProcessor"></param>
		/// <param name="taskManager"></param>
		/// <param name="processEnvironment"></param>
		public ProcessTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable = null,
			string arguments = null,
			IOutputProcessor<T> outputProcessor = null
			)
			 : this(taskManager, taskManager?.Token ?? default, processEnvironment, executable, arguments, outputProcessor)
		{}

		/// <summary>
		/// Runs a Process with the passed arguments
		/// </summary>
		/// <param name="token"></param>
		/// <param name="executable"></param>
		/// <param name="arguments"></param>
		/// <param name="outputProcessor"></param>
		public ProcessTask(ITaskManager taskManager,
			CancellationToken token,
			IProcessEnvironment processEnvironment,
			string executable = null,
			string arguments = null,
			IOutputProcessor<T> outputProcessor = null
			)
			 : base(taskManager, token)
		{
			this.outputProcessor = outputProcessor;
			ProcessEnvironment = processEnvironment;
			ProcessArguments = arguments;
			ProcessName = executable;
		}

		public virtual void Configure(ProcessStartInfo psi)
		{
			Guard.ArgumentNotNull(psi, "psi");

			ConfigureOutputProcessor();

			Guard.NotNull(this, outputProcessor, nameof(outputProcessor));
			Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			ProcessName = psi.FileName;
			Name = ProcessArguments;
		}

		public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor)
		{
			outputProcessor = processor ?? outputProcessor;
			ConfigureOutputProcessor();

			Guard.NotNull(this, outputProcessor, nameof(outputProcessor));

			Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			ProcessName = psi.FileName;
			Name = ProcessArguments;
		}

		public void Configure(Process existingProcess)
		{
			Guard.ArgumentNotNull(existingProcess, "existingProcess");

			ConfigureOutputProcessor();

			Guard.NotNull(this, outputProcessor, nameof(outputProcessor));

			Process = existingProcess;
			ProcessName = existingProcess.StartInfo.FileName;
			Name = ProcessArguments;
		}

		public new IProcessTask<T> Start()
		{
			base.Start();
			return this;
		}

		IProcessTask IProcessTask.Start()
		{
			return Start();
		}

		public void Stop()
		{
			wrapper?.Stop();
		}

		public override string ToString()
		{
			return $"{Task?.Id ?? -1} {Name} {GetType()} {ProcessName} {ProcessArguments}";
		}

		protected override void RaiseOnEnd()
		{
			base.RaiseOnEnd();
			OnEndProcess?.Invoke(this);
		}

		protected virtual void ConfigureOutputProcessor()
		{
		}

		protected override T RunWithReturn(bool success)
		{
			var result = base.RunWithReturn(success);

			wrapper = new ProcessWrapper(Name, Process, outputProcessor,
				 () => OnStartProcess?.Invoke(this),
				 () => {
					 try
					 {
						 if (outputProcessor != null)
							 result = outputProcessor.Result;

						 if (typeof(T) == typeof(string) && result == null && !Process.StartInfo.CreateNoWindow)
							 result = (T)(object)"Process running";

						 if (!String.IsNullOrEmpty(Errors))
							 OnErrorData?.Invoke(Errors);
					 }
					 catch (Exception ex)
					 {
						 if (thrownException == null)
							 thrownException = new ProcessException(ex.Message, ex);
						 else
							 thrownException = new ProcessException(thrownException.GetExceptionMessage(), ex);
					 }

					 if (thrownException != null && !RaiseFaultHandlers(thrownException))
						 ThrownException.Rethrow();
				 },
				 (ex, error) => {
					 thrownException = ex;
					 Errors = error;
				 },
				 Token);

			wrapper.Run();

			return result;
		}

		public IProcessEnvironment ProcessEnvironment { get; private set; }
		public Process Process { get; set; }
		public int ProcessId => wrapper.ProcessId;
		public override bool Successful => base.Successful && wrapper.ExitCode == 0;
		public StreamWriter StandardInput => wrapper?.Input;
		public virtual string ProcessName { get; protected set; }
		public virtual string ProcessArguments { get; }
	}

	public class ProcessTaskWithListOutput<T> : DataTaskBase<T, List<T>>, IProcessTask<T, List<T>>
	{
		private readonly bool longRunning;
		private IOutputProcessor<T, List<T>> outputProcessor;
		private Exception thrownException = null;
		private ProcessWrapper wrapper;
		public event Action<IProcess> OnEndProcess;

		public event Action<string> OnErrorData;
		public event Action<IProcess> OnStartProcess;

		protected ProcessTaskWithListOutput() {}

		public ProcessTaskWithListOutput(
			ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable = null,
			string arguments = null,
			IOutputProcessor<T, List<T>> outputProcessor = null,
			bool longRunning = false)
			 : this(taskManager, taskManager?.Token ?? default, processEnvironment, executable, arguments, outputProcessor, longRunning)
		{}

		public ProcessTaskWithListOutput(
			ITaskManager taskManager,
			CancellationToken token,
			IProcessEnvironment processEnvironment,
			string executable = null,
			string arguments = null,
			IOutputProcessor<T, List<T>> outputProcessor = null,
			bool longRunning = false)
			 : base(taskManager, token)
		{
			this.outputProcessor = outputProcessor;
			ProcessEnvironment = processEnvironment;
			this.longRunning = longRunning;
			ProcessArguments = arguments;
			ProcessName = executable;
		}

		public virtual void Configure(ProcessStartInfo psi)
		{
			Guard.ArgumentNotNull(psi, "psi");

			ConfigureOutputProcessor();

			Guard.NotNull(this, outputProcessor, nameof(outputProcessor));

			Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			ProcessName = psi.FileName;
		}

		public void Configure(Process existingProcess)
		{
			Guard.ArgumentNotNull(existingProcess, "existingProcess");

			ConfigureOutputProcessor();
			Guard.NotNull(this, outputProcessor, nameof(outputProcessor));
			Process = existingProcess;
			ProcessName = existingProcess.StartInfo.FileName;
		}

		public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<T, List<T>> processor)
		{
			Guard.ArgumentNotNull(psi, "psi");
			Guard.ArgumentNotNull(processor, "processor");

			outputProcessor = processor ?? outputProcessor;
			ConfigureOutputProcessor();
			Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			ProcessName = psi.FileName;
		}

		IProcessTask IProcessTask.Start()
		{
			Start();
			return this;
		}

		public new IProcessTask<T, List<T>> Start()
		{
			base.Start();
			return this;
		}

		public void Stop()
		{
			wrapper?.Stop();
		}

		public override string ToString()
		{
			return $"{Task?.Id ?? -1} {Name} {GetType()} {ProcessName} {ProcessArguments}";
		}

		protected override void RaiseOnEnd()
		{
			base.RaiseOnEnd();
			OnEndProcess?.Invoke(this);
		}

		protected virtual void ConfigureOutputProcessor()
		{
			if (outputProcessor == null && (typeof(T) != typeof(string)))
			{
				throw new InvalidOperationException("ProcessTask without an output processor must be defined as IProcessTask<string>");
			}
			outputProcessor.OnEntry += x => RaiseOnData(x);
		}

		protected override List<T> RunWithReturn(bool success)
		{
			var result = base.RunWithReturn(success);

			wrapper = new ProcessWrapper(Name, Process, outputProcessor,
				 onStart: () => OnStartProcess?.Invoke(this),
				 onEnd: () => {
					 try
					 {
						 if (outputProcessor != null)
							 result = outputProcessor.Result;
						 if (result == null)
							 result = new List<T>();

						 if (!String.IsNullOrEmpty(Errors))
							 OnErrorData?.Invoke(Errors);
					 }
					 catch (Exception ex)
					 {
						 if (thrownException == null)
							 thrownException = new ProcessException(ex.Message, ex);
						 else
							 thrownException = new ProcessException(thrownException.GetExceptionMessage(), ex);
					 }

					 if (thrownException != null && !RaiseFaultHandlers(thrownException))
						 ThrownException.Rethrow();
				 },
				 onError: (ex, error) => {
					 thrownException = ex;
					 Errors = error;
				 },
				 token: Token);
			wrapper.Run();

			return result;
		}

		public IProcessEnvironment ProcessEnvironment { get; private set; }
		public Process Process { get; set; }
		public int ProcessId => wrapper.ProcessId;
		public override bool Successful => base.Successful && wrapper.ExitCode == 0;
		public StreamWriter StandardInput => wrapper?.Input;
		public virtual string ProcessName { get; protected set; }
		public virtual string ProcessArguments { get; }

		IProcessEnvironment IProcessTask.ProcessEnvironment => throw new NotImplementedException();

		StreamWriter IProcess.StandardInput => throw new NotImplementedException();

		int IProcess.ProcessId => throw new NotImplementedException();

		string IProcess.ProcessName => throw new NotImplementedException();

		string IProcess.ProcessArguments => throw new NotImplementedException();

		Process IProcess.Process { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	}


	public class ProcessTaskLongRunning : TaskBase, IProcessTask
	{
		private Exception thrownException = null;
		private ProcessWrapper wrapper;
		public event Action<IProcess> OnEndProcess;

		public event Action<string> OnErrorData;
		public event Action<IProcess> OnStartProcess;

		protected ProcessTaskLongRunning() {}

		public ProcessTaskLongRunning(
			ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable = null,
			string arguments = null)
			 : this(taskManager, taskManager?.Token ?? default, processEnvironment, executable, arguments)
		{}

		public ProcessTaskLongRunning(
			ITaskManager taskManager,
			CancellationToken token,
			IProcessEnvironment processEnvironment,
			string executable = null,
			string arguments = null)
			 : base(taskManager, token)
		{
			ProcessEnvironment = processEnvironment;
			ProcessArguments = arguments;
			ProcessName = executable;
		}

		public virtual void Configure(ProcessStartInfo psi)
		{
			Guard.ArgumentNotNull(psi, "psi");

			Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			ProcessName = psi.FileName;
		}

		public void Configure(Process existingProcess)
		{
			throw new NotImplementedException();
		}

		public new IProcessTask Start()
		{
			base.Start();
			return this;
		}

		public void Stop()
		{
			wrapper?.Stop();
		}

		public override string ToString()
		{
			return $"{Task?.Id ?? -1} {Name} {GetType()} {ProcessName} {ProcessArguments}";
		}

		protected override void RaiseOnEnd()
		{
			base.RaiseOnEnd();
			OnEndProcess?.Invoke(this);
		}

		protected override void Run(bool success)
		{
			wrapper = new ProcessWrapper(Name, Process,
				 onStart: () => OnStartProcess?.Invoke(this),
				 onEnd: () => {
					 try
					 {
						 if (!string.IsNullOrEmpty(Errors))
							 OnErrorData?.Invoke(Errors);
					 }
					 catch (Exception ex)
					 {
						 if (thrownException == null)
							 thrownException = new ProcessException(ex.Message, ex);
						 else
							 thrownException = new ProcessException(thrownException.GetExceptionMessage(), ex);
					 }

					 if (thrownException != null && !RaiseFaultHandlers(thrownException))
						 ThrownException.Rethrow();
				 },
				 onError: (ex, error) => {
					 thrownException = ex;
					 Errors = error;
				 },
				 token: Token);

			wrapper.Run();
		}

		public IProcessEnvironment ProcessEnvironment { get; private set; }
		public Process Process { get; set; }
		public int ProcessId => wrapper.ProcessId;
		public override bool Successful => base.Successful && wrapper.ExitCode == 0;
		public StreamWriter StandardInput => wrapper?.Input;
		public virtual string ProcessName { get; protected set; }
		public virtual string ProcessArguments { get; }

		public override TaskAffinity Affinity => TaskAffinity.LongRunning;
	}
}
