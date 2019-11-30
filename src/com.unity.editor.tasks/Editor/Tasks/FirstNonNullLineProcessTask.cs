namespace Unity.Editor.Tasks
{
	public class FirstNonNullLineProcessTask : SimpleProcessTask
	{
		public FirstNonNullLineProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			string workingDirectory = null)
			: base(taskManager, processManager, executable, arguments, workingDirectory,
				new FirstNonNullLineOutputProcessor<string>())
		{}
	}
}
