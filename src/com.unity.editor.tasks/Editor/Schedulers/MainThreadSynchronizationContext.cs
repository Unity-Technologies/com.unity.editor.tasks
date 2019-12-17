using System;
using System.Threading;

namespace Unity.Editor.Tasks
{
	using Helpers;

#if UNITY_EDITOR

	using UnityEditor;

	public class MainThreadSynchronizationContext: SynchronizationContext, IMainThreadSynchronizationContext
	{
		public MainThreadSynchronizationContext(CancellationToken token = default) { }

		public void Schedule(Action action)
		{
			action.EnsureNotNull(nameof(action));
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
		public MainThreadSynchronizationContext(CancellationToken token = default) : base(token) {}

		public void Schedule(Action action)
		{
			action.EnsureNotNull(nameof(action));
			Post(act => ((Action)act)(), action);
		}
	}
#endif
}
