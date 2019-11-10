// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using Helpers;
	public class ThreadingHelper
	{
		public void SetUIThread()
		{
			MainThread = Thread.CurrentThread.ManagedThreadId;
		}

		public static TaskScheduler GetUIScheduler(SynchronizationContext synchronizationContext)
		{
			// quickly swap out the sync context so we can leverage FromCurrentSynchronizationContext for our ui scheduler
			var currentSyncContext = SynchronizationContext.Current;
			SynchronizationContext.SetSynchronizationContext(synchronizationContext);
			var ret = TaskScheduler.FromCurrentSynchronizationContext();
			if (currentSyncContext != null)
				SynchronizationContext.SetSynchronizationContext(currentSyncContext);
			return ret;
		}

		public int MainThread { get; set; }
		bool InMainThread { get { return MainThread == 0 || Thread.CurrentThread.ManagedThreadId == MainThread; } }

		public bool InUIThread => InMainThread || Guard.InUnitTestRunner;
	}
}
