using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using System;

	public class ThreadSynchronizationContext : SynchronizationContext, IDisposable
	{
		private readonly ConcurrentQueue<PostData> priorityQueue = new ConcurrentQueue<PostData>();
		private readonly ConcurrentQueue<PostData> queue = new ConcurrentQueue<PostData>();
        private readonly ManualResetEventSlim dataSignal = new ManualResetEventSlim(false);
		private readonly CancellationTokenSource cts = new CancellationTokenSource();
		protected WaitHandle Completion => new ManualResetEvent(false);

        private int threadId;
        protected bool IsInSyncThread => Thread.CurrentThread.ManagedThreadId == threadId;
		

		public ThreadSynchronizationContext(CancellationToken token)
		{
			token.Register(Stop);
			Task.Factory.StartNew(Start, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		public void Stop()
		{
			cts.Cancel();
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			var data = new PostData { Completion = new ManualResetEventSlim(), Callback = d, State = state };
			queue.Enqueue(data);
            lock (dataSignal)
            {
                dataSignal.Set();
            }
		}

		public override void Send(SendOrPostCallback d, object state)
		{
			if (IsInSyncThread)
			{
				d(state);
			}
			else
            {
                var data = new PostData { Completion = new ManualResetEventSlim(), Callback = d, State = state };
                priorityQueue.Enqueue(data);
                lock (dataSignal)
                {
                    dataSignal.Set();
                }
				data.Completion.Wait(cts.Token);
			}
		}

		private void Pump()
		{
            lock (dataSignal)
            {
                dataSignal.Reset();
            }

            PostData data;
			while (priorityQueue.TryDequeue(out data))
			{
				if (cts.Token.IsCancellationRequested) return;
				data.Run();
			}

            while (queue.TryDequeue(out data))
			{
				if (cts.Token.IsCancellationRequested) return;
				data.Run();
			}
		}

		private void Start()
		{
			SetSynchronizationContext(this);

			threadId = Thread.CurrentThread.ManagedThreadId;

			while (!cts.Token.IsCancellationRequested)
			{
				Pump();
                dataSignal.Wait(cts.Token);
            }

			((ManualResetEvent)Completion).Set();
		}

		private bool disposed;

		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;
			if (disposing)
			{
				Stop();
				if (!IsInSyncThread)
				{
					Completion.WaitOne();
				}
				cts.Dispose();
				dataSignal.Dispose();
				disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		struct PostData
		{
			public ManualResetEventSlim Completion;
			public SendOrPostCallback Callback;
			public object State;

			public void Run()
			{
				if (Completion.IsSet)
					return;

				try
				{
					Callback(State);
				}
				catch { }
				Completion.Set();
			}
		}
	}
}
