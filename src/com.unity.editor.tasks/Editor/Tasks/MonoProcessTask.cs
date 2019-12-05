// Copyright 2019 Unity
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using Unity.Editor.Tasks.Helpers;

namespace Unity.Editor.Tasks
{
	/// <summary>
	/// Runs a dotnet process on mono, using Unity's mono.
	/// </summary>
	public class MonoProcessTask : DotNetProcessTask
	{
		/// <summary>
		/// Runs a dotnet process on mono, using Unity's mono. You don't need to call Configure on this
		/// task, it already does it.
		/// </summary>
		public MonoProcessTask(ITaskManager taskManager, IProcessManager processManager,
				string executable, string arguments,
				string workingDirectory = null)
			: base(taskManager, processManager.DefaultProcessEnvironment,
					processManager, processManager.DefaultProcessEnvironment.Environment,
					executable, arguments, workingDirectory, true)
		{}

		/// <summary>
		/// Runs a dotnet process on mono, using Unity's mono. You don't need to call Configure on this
		/// task, it already does it.
		/// </summary>
		public MonoProcessTask(ITaskManager taskManager, IProcessManager processManager,
				IEnvironment environment, IProcessEnvironment processEnvironment,
				string executable, string arguments,
				string workingDirectory = null)
			: base(taskManager, processEnvironment,
				processManager, environment,
				executable, arguments, workingDirectory, true)
		{}
	}

	/// <summary>
	/// Runs a dotnet process on mono, using Unity's mono.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class MonoProcessTask<T> : DotNetProcessTask<T>
	{
		/// <summary>
		/// Runs a dotnet process on mono, using Unity's mono. You don't need to call Configure on this
		/// task, it already does it.
		/// </summary>
		public MonoProcessTask(ITaskManager taskManager, IProcessManager processManager,
				string executable, string arguments,
				IOutputProcessor<T> outputProcessor,
				string workingDirectory = null)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
					processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
					executable, arguments, workingDirectory, outputProcessor, false)
		{ }

		/// <summary>
		/// Runs a dotnet process on mono, using Unity's mono. You don't need to call Configure on this
		/// task, it already does it.
		/// </summary>
		public MonoProcessTask(ITaskManager taskManager, IProcessManager processManager,
				IEnvironment environment, IProcessEnvironment processEnvironment,
				string executable, string arguments,
				IOutputProcessor<T> outputProcessor,
				string workingDirectory = null)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
				  executable, arguments, workingDirectory, outputProcessor, false)
		{ }

		/// <summary>
		/// Runs a dotnet process on mono, using Unity's mono. You don't need to call Configure on this
		/// task, it already does it.
		/// </summary>
		public MonoProcessTask(ITaskManager taskManager, IProcessManager processManager,
				string executable, string arguments,
				Func<IProcessTask<T>, string, bool> isMatch,
				Func<IProcessTask<T>, string, T> processor,
				string workingDirectory = null)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
					processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
					executable, arguments, workingDirectory, isMatch, processor, false)
		{ }

		/// <summary>
		/// Runs a dotnet process on mono, using Unity's mono. You don't need to call Configure on this
		/// task, it already does it.
		/// </summary>
		public MonoProcessTask(ITaskManager taskManager, IProcessManager processManager,
				IEnvironment environment, IProcessEnvironment processEnvironment,
				string executable, string arguments,
				Func<IProcessTask<T>, string, bool> isMatch,
				Func<IProcessTask<T>, string, T> processor,
				string workingDirectory = null)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
					processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
					executable, arguments, workingDirectory, isMatch, processor, false)
		{
		}
	}
}
