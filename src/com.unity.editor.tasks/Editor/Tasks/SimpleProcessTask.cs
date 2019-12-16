namespace Unity.Editor.Tasks
{
	using System;
	using System.Collections.Generic;
	using Helpers;

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
				processor ?? new StringListOutputProcessor())
		{
			processManager.Configure(this, workingDirectory);
		}

		public override TaskAffinity Affinity { get; set; } = TaskAffinity.None;
	}

}
