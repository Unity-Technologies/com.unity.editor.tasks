// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Unity.Editor.Tasks
{
	using Extensions;
	using Unity.Editor.Tasks.Helpers;

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

	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T">The type of the results. If it's a List<> or similar, then specify the full List<> type here and the inner type of the List in <typeparam name="TData"/>
	/// <typeparam name="TData">If <typeparam name="TData"/> is a list or similar, then specify its inner type here</typeparam>
	public class ProcessTask<T> : TaskBase<T>, IProcessTask<T>, IDisposable
	{
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
			OutputProcessor = outputProcessor;
			ProcessEnvironment = processEnvironment;
			ProcessArguments = arguments;
			ProcessName = executable;
		}

		public virtual void Configure(ProcessStartInfo psi)
		{
			psi.EnsureNotNull(nameof(psi));

			ConfigureOutputProcessor();

			this.EnsureNotNull(OutputProcessor, nameof(OutputProcessor));

			Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			ProcessName = psi.FileName;
			Name = ProcessArguments;
		}

		public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor)
		{
			OutputProcessor = processor ?? OutputProcessor;
			ConfigureOutputProcessor();

			this.EnsureNotNull(OutputProcessor, nameof(OutputProcessor));

			Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			ProcessName = psi.FileName;
			Name = ProcessArguments;
		}

		public void Configure(Process existingProcess)
		{
			existingProcess.EnsureNotNull(nameof(existingProcess));

			ConfigureOutputProcessor();

			this.EnsureNotNull(OutputProcessor, nameof(OutputProcessor));

			Process = existingProcess;
			ProcessName = Process.StartInfo.FileName;
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

			wrapper = new ProcessWrapper(Name, Process, OutputProcessor, Affinity == TaskAffinity.LongRunning,
				 () => OnStartProcess?.Invoke(this),
				 () => {
					 try
					 {
						 if (OutputProcessor != null)
							 result = OutputProcessor.Result;

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

		private bool disposed;
		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;
			if (disposing)
			{
				wrapper?.Dispose();
				disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public IProcessEnvironment ProcessEnvironment { get; private set; }
		public Process Process { get; set; }
		public int ProcessId => wrapper.ProcessId;
		public override bool Successful => base.Successful && wrapper.ExitCode == 0;
		public StreamWriter StandardInput => wrapper?.Input;
		public virtual string ProcessName { get; protected set; }
		public virtual string ProcessArguments { get; }

		protected IOutputProcessor<T> OutputProcessor { get; set; }
	}

	public class ProcessTaskWithListOutput<T> : DataTaskBase<T, List<T>>, IProcessTask<T, List<T>>, IDisposable
	{
		private readonly bool longRunning;
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
			this.OutputProcessor = outputProcessor;
			ProcessEnvironment = processEnvironment;
			this.longRunning = longRunning;
			ProcessArguments = arguments;
			ProcessName = executable;
		}

		public virtual void Configure(ProcessStartInfo psi)
		{
			psi.EnsureNotNull(nameof(psi));

			ConfigureOutputProcessor();

			this.EnsureNotNull(OutputProcessor, nameof(OutputProcessor));

			Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
			ProcessName = psi.FileName;
		}

		public void Configure(Process existingProcess)
		{
			Guard.EnsureNotNull(existingProcess, "existingProcess");

			ConfigureOutputProcessor();

			this.EnsureNotNull(OutputProcessor, nameof(OutputProcessor));

			Process = existingProcess;
			ProcessName = existingProcess.StartInfo.FileName;
		}

		public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<T, List<T>> processor)
		{
			psi.EnsureNotNull(nameof(psi));
			processor.EnsureNotNull(nameof(processor));

			OutputProcessor = processor ?? OutputProcessor;

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
			if (OutputProcessor == null && (typeof(T) != typeof(string)))
			{
				throw new InvalidOperationException("ProcessTask without an output processor must be defined as IProcessTask<string>");
			}
			OutputProcessor.OnEntry += x => RaiseOnData(x);
		}

		protected override List<T> RunWithReturn(bool success)
		{
			var result = base.RunWithReturn(success);

			wrapper = new ProcessWrapper(Name, Process, OutputProcessor, Affinity == TaskAffinity.LongRunning,
				 onStart: () => OnStartProcess?.Invoke(this),
				 onEnd: () => {
					 try
					 {
						 if (OutputProcessor != null)
							 result = OutputProcessor.Result;
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

		private bool disposed;
		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;
			if (disposing)
			{
				wrapper?.Dispose();
				disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		public IProcessEnvironment ProcessEnvironment { get; private set; }
		public Process Process { get; set; }
		public int ProcessId => wrapper.ProcessId;
		public override bool Successful => base.Successful && wrapper.ExitCode == 0;
		public StreamWriter StandardInput => wrapper?.Input;
		public virtual string ProcessName { get; protected set; }
		public virtual string ProcessArguments { get; }

		protected IOutputProcessor<T, List<T>> OutputProcessor { get; private set; }
	}
}
