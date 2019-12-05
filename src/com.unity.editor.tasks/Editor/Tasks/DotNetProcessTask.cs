// Copyright 2019 Unity
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Unity.Editor.Tasks
{
	using System;
	using Helpers;
	using Internal.IO;

	/// <summary>
	/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
	/// it runs the executable using Unity's mono.
	/// </summary>
	public class DotNetProcessTask : DotNetProcessTask<string>
	{
		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager, IProcessManager processManager,
				string executable, string arguments,
				string workingDirectory = null)
			: this(taskManager, processManager.DefaultProcessEnvironment,
				  processManager, processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, false)
		{}

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager, IProcessManager processManager,
			IEnvironment environment, IProcessEnvironment processEnvironment,
			string executable, string arguments,
			string workingDirectory = null)
			: this(taskManager, processEnvironment,
				processManager, environment,
				executable, arguments, workingDirectory, false)
		{}

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		protected DotNetProcessTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
			IProcessManager processManager, IEnvironment environment,
			string executable, string arguments,
			string workingDirectory,
			bool alwaysUseMono)
			: base(taskManager, processEnvironment, new SimpleOutputProcessor())
		{
			if (!alwaysUseMono && environment.IsWindows)
			{
				ProcessName = executable;
				ProcessArguments = arguments;
			}
			else
			{
				ProcessArguments = executable + " " + arguments;
				ProcessName = environment.UnityApplicationContents.ToSPath()
										.Combine("MonoBleedingEdge", "bin", "mono" + environment.ExecutableExtension);
			}
			processManager.Configure(this, workingDirectory);
		}
	}

	/// <summary>
	/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
	/// it runs the executable using Unity's mono.
	/// </summary>
	public class DotNetProcessTask<T> : ProcessTask<T>
	{
		private readonly Func<IProcessTask<T>, string, bool> isMatch;
		private readonly Func<IProcessTask<T>, string, T> processor;

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			string workingDirectory = null)
			: this(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, outputProcessor, false)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			IEnvironment environment,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			string workingDirectory = null)
			: this(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
				  executable, arguments, workingDirectory, outputProcessor, false)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null)
			: this(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, isMatch, processor, false)
		{}

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			IEnvironment environment,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null)
			: this(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
				  executable, arguments, workingDirectory, isMatch, processor, false)
		{
		}

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		protected DotNetProcessTask(ITaskManager taskManager, IProcessManager processManager, IProcessEnvironment processEnvironment, IEnvironment environment,
			string executable, string arguments, string workingDirectory,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			bool alwaysUseMono)
			: base(taskManager, processEnvironment)
		{
			this.isMatch = isMatch;
			this.processor = processor;

			processManager.EnsureNotNull(nameof(processManager));

			if (!alwaysUseMono && environment.IsWindows)
			{
				ProcessName = executable;
				ProcessArguments = arguments;
			}
			else
			{
				ProcessArguments = executable + " " + arguments;
				ProcessName = environment.UnityApplicationContents.ToSPath()
										.Combine("MonoBleedingEdge", "bin", "mono" + environment.ExecutableExtension);
			}

			processManager.Configure(this, workingDirectory);
		}

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		protected DotNetProcessTask(ITaskManager taskManager, IProcessManager processManager, IProcessEnvironment processEnvironment, IEnvironment environment,
			string executable, string arguments, string workingDirectory,
			IOutputProcessor<T> outputProcessor, bool alwaysUseMono)
			: base(taskManager, processEnvironment, outputProcessor: outputProcessor)
		{
			processManager.EnsureNotNull(nameof(processManager));

			if (!alwaysUseMono && environment.IsWindows)
			{
				ProcessName = executable;
				ProcessArguments = arguments;
			}
			else
			{
				ProcessArguments = executable + " " + arguments;
				ProcessName = environment.UnityApplicationContents.ToSPath()
										.Combine("MonoBleedingEdge", "bin", "mono" + environment.ExecutableExtension);
			}

			processManager.Configure(this, workingDirectory);
		}

		protected DotNetProcessTask(ITaskManager taskManager, IProcessEnvironment processEnvironment, IOutputProcessor<T> outputProcessor)
			: base(taskManager, processEnvironment, outputProcessor: outputProcessor)
		{}

		protected override void ConfigureOutputProcessor()
		{
			if (OutputProcessor == null && processor != null)
			{
				OutputProcessor = new BaseOutputProcessor<T>((string line, out T result) => {
					result = default(T);
					if (!isMatch(this, line)) return false;
					result = processor(this, line);
					return true;
				});
			}

			base.ConfigureOutputProcessor();
		}
	}
}
