namespace Unity.Editor.Tasks
{
	public enum TaskAffinity
	{
		Concurrent,
		Exclusive,
		UI,
		LongRunning,
		ThreadPool
	}
}
