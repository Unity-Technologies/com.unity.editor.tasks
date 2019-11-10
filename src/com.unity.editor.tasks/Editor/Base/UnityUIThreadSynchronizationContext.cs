using System;
using System.Threading;
using UnityEditor;

namespace Unity.Editor.Tasks
{
	using Helpers;

	public interface IMainThreadSynchronizationContext
	{
		void Schedule(Action action);
	}

	public class UnityUIThreadSynchronizationContext : SynchronizationContext, IMainThreadSynchronizationContext
	{
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
	}
}
