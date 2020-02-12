using System;
using System.Threading;

namespace Unity.Editor.Tasks
{
	using Helpers;

#if UNITY_EDITOR
	using System.Collections.Generic;
	using UnityEditor;

	public class MainThreadSynchronizationContext: SynchronizationContext, IMainThreadSynchronizationContext
	{
		readonly Queue<Action> m_Callbacks = new Queue<Action>();

		public MainThreadSynchronizationContext(CancellationToken token = default)
		{
			EditorApplication.update += Update;
		}

		public void Dispose()
		{
			EditorApplication.update -= Update;
		}

		public void Schedule(Action action)
		{
			action.EnsureNotNull(nameof(action));
			Post(act => ((Action)act)(), action);
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			if (d == null)
				return;

			lock (m_Callbacks)
				m_Callbacks.Enqueue(() => d(state));
		}

		void Update()
		{
			Queue<Action> callbacks;

			lock (m_Callbacks)
			{
				if (m_Callbacks.Count == 0)
					return;
				callbacks = new Queue<Action>(m_Callbacks);
				m_Callbacks.Clear();
			}

			foreach (var callback in callbacks)
				callback();
		}
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
