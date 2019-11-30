namespace Unity.Editor.Tasks
{
	using Helpers;

	public class FindExecTask : FirstNonNullLineProcessTask
	{
		public FindExecTask(ITaskManager taskManager, IProcessManager processManager, string execToFind)
			: base(taskManager, processManager,
				Guard.EnsureNotNull(processManager, nameof(processManager)).DefaultProcessEnvironment.Environment.IsWindows ? "where" : "which",
				execToFind)
		{}

		public override TaskAffinity Affinity => TaskAffinity.None;
	}
}
