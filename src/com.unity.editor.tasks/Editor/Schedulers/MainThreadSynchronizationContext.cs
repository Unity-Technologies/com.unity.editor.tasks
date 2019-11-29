using System;
using System.Threading;

namespace Unity.Editor.Tasks
{
	using Helpers;

#if UNITY_EDITOR

	using Helpers;

	public class MainThreadSynchronizationContext: SynchronizationContext, IMainThreadSynchronizationContext
	{
		public MainThreadSynchronizationContext(CancellationToken token) {}

		public void Schedule(Action action)
		{
			Guard.ArgumentNotNull(action, "action");
			Post(act => ((Action)act)(), action);
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			if (d == null)
				return;

			EditorApplication.delayCall += () => d(state);
		}

		public void Dispose() {}
	}
#else
	public class MainThreadSynchronizationContext : ThreadSynchronizationContext, IMainThreadSynchronizationContext
	{
		public MainThreadSynchronizationContext() : base(default) {}
		public MainThreadSynchronizationContext(CancellationToken token) : base(token) {}

		public void Schedule(Action action)
		{
			Guard.EnsureNotNull(action, "action");
			Post(act => ((Action)act)(), action);
		}
	}
#endif
}
