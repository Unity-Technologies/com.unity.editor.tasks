namespace Unity.Editor.Tasks
{

	/// <summary>
	/// A process manager that configures processes for running and keeps track of running processes.
	/// </summary>
	public interface IProcessManager
	{
		/// <summary>
		/// Helper that configures all the necessary parts in order to run a process. This must be called before running
		/// a ProcessTask.
		/// </summary>
		T Configure<T>(T processTask, string workingDirectory = null) where T : IProcessTask;

		/// <summary>
		/// Stops all running processes managed by this manager.
		/// </summary>
		void Stop();

		/// <summary>
		/// The default process environment, with default working directory.
		/// </summary>
		IProcessEnvironment DefaultProcessEnvironment { get; }
	}
}
