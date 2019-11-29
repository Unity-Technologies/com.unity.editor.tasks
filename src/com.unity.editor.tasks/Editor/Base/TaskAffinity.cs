namespace Unity.Editor.Tasks
{
	/// <summary>
	/// Thread affinity of a task.
	/// </summary>
	public enum TaskAffinity
	{
		/// <summary>
		/// Tasks with this affinity will only run when there are no Exclusive affinity tasks running.
		/// <see cref="ITask.Then" /> task handlers run with this affinity by default
		/// Reader side of the Writer-Reader pair of schedulers
		/// </summary>
		Concurrent,
		/// <summary>
		/// Tasks with this affinity will run one at a time, and no Concurrent affinity tasks will run at the same time as these
		/// Writer side of the Writer-Reader pair of schedulers
		/// </summary>
		Exclusive,
		/// <summary>
		/// Tasks with this affinity will run on the UI thread (the thread that the task manager was initialized on
		/// TaskExtensions.ThenInUI and TaskExtensions.FinallyInUI task handlers run with this affinity by default
		/// </summary>
		UI,
		/// <summary>
		/// Tasks with this affinity are long-lived and not expected to return until the app shuts down or something fails.
		/// They'll run on the thread pool, and if it's a ProcessTask, it won't exit until the underlying process stops or
		/// the Task is explicitely stopped.
		/// </summary>
		LongRunning,
		/// <summary>
		/// Tasks with this affinity run in the thread pool. <see cref="ITask.Finally" /> task handlers run with this affinity by default.
		/// </summary>
		None
	}
}
