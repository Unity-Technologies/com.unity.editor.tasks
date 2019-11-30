namespace Unity.Editor.Tasks
{
	using System;
	using System.Collections.Generic;
	using Helpers;

	public class SimpleProcessTask : ProcessTask<string>
	{
		public SimpleProcessTask(ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments, string workingDirectory = null,
			IOutputProcessor<string> processor = null)
			: base(taskManager,
				processManager.EnsureNotNull(nameof(processManager)).DefaultProcessEnvironment,
				executable, arguments, processor ?? new SimpleOutputProcessor())
		{
			processManager.Configure(this, workingDirectory);
		}

		public override TaskAffinity Affinity { get; set; } = TaskAffinity.None;
	}

	public class SimpleProcessTask<T> : ProcessTask<T>
	{
		public SimpleProcessTask(
			ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments,
			Func<string, T> processor,
			string workingDirectory = null
		)
			: base(taskManager, taskManager?.Token ?? default,
				processManager.EnsureNotNull(nameof(processManager)).DefaultProcessEnvironment,
				executable, arguments,
				new BaseOutputProcessor<T>((string line, out T result) => {
					result = default(T);
					if (line == null) return false;
					result = processor(line);
					return true;
				})
			)
		{
			processManager.Configure(this, workingDirectory);
		}

		public SimpleProcessTask(
			ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments,
			IOutputProcessor<T> outputProcessor,
			string workingDirectory = null
		)
			: base(taskManager, taskManager?.Token ?? default,
				processManager.EnsureNotNull(nameof(processManager)).DefaultProcessEnvironment,
				executable, arguments, outputProcessor)
		{
			processManager.Configure(this, workingDirectory);
		}

		public override TaskAffinity Affinity { get; set; } = TaskAffinity.None;
	}

	public class SimpleListProcessTask : ProcessTaskWithListOutput<string>
	{
		public SimpleListProcessTask(
			ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments, string workingDirectory = null,
			IOutputProcessor<string, List<string>> processor = null
		)
			: base(taskManager, taskManager?.Token ?? default,
				processManager.EnsureNotNull(nameof(processManager)).DefaultProcessEnvironment,
				executable, arguments,
				processor ?? new SimpleListOutputProcessor())
		{
			processManager.Configure(this, workingDirectory);
		}

		public override TaskAffinity Affinity { get; set; } = TaskAffinity.None;
	}

}
