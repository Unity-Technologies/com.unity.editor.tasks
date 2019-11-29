// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using Logging;
	using Helpers;
	public enum TaskRunOptions
	{
		OnSuccess,
		OnFailure,
		OnAlways
	}

	public interface ITask : IAsyncResult
	{
		event Action<ITask, bool, Exception> OnEnd;
		event Action<ITask> OnStart;
		T Then<T>(T continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false) where T : ITask;
		ITask Catch(Action<Exception> handler);
		ITask Catch(Func<Exception, bool> handler);

		/// <summary>
		/// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
		/// </summary>
		ITask FinallyInline(Action<bool> handler);

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		ITask Finally(Action<bool, Exception> actionToContinueWith, string name = null, TaskAffinity affinity = TaskAffinity.ThreadPool);

		/// <summary>
		/// Run another task at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		T Finally<T>(T taskToContinueWith) where T : ITask;

		ITask Start();
		void RunSynchronously();

		ITask Progress(Action<IProgress> progressHandler);
		void UpdateProgress(long value, long total, string message = null);

		ITask GetTopOfChain(bool onlyCreated = true);
		ITask GetEndOfChain();

		/// <summary>
		/// </summary>
		/// <returns>true if any task on the chain is marked as exclusive</returns>
		bool IsChainExclusive();

		bool Successful { get; }
		string Errors { get; }
		Task Task { get; }
		string Name { get; }
		TaskAffinity Affinity { get; set; }
		CancellationToken Token { get; }
		ITaskManager TaskManager { get; }
		TaskBase DependsOn { get; }
		string Message { get; }
		Exception Exception { get; }
	}

	public interface ITask<TResult> : ITask
	{
		new event Action<ITask<TResult>, TResult, bool, Exception> OnEnd;
		new event Action<ITask<TResult>> OnStart;
		new ITask<TResult> Catch(Action<Exception> handler);
		new ITask<TResult> Catch(Func<Exception, bool> handler);

		/// <summary>
		/// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
		/// </summary>
		ITask<TResult> FinallyInline(Action<bool, TResult> handler);

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		ITask<TResult> Finally(Func<bool, Exception, TResult, TResult> continuation, string name = null, TaskAffinity affinity = TaskAffinity.ThreadPool);

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		ITask Finally(Action<bool, Exception, TResult> continuation, string name = null, TaskAffinity affinity = TaskAffinity.ThreadPool);

		new ITask<TResult> Start();
		new TResult RunSynchronously();
		new ITask<TResult> Progress(Action<IProgress> progressHandler);

		TResult Result { get; }
		new Task<TResult> Task { get; }
	}

	public interface ITask<TData, T> : ITask<T>
	{
		event Action<TData> OnData;
	}

	public class TaskData : ITask
	{
		public Progress progress;

		event Action<ITask, bool, Exception> ITask.OnEnd
		{
			add => throw new NotImplementedException();
			remove => throw new NotImplementedException();
		}

		event Action<ITask> ITask.OnStart
		{
			add => throw new NotImplementedException();
			remove => throw new NotImplementedException();
		}

		public TaskData(string name, long total)
		{
			Message = name;
			Name = name;
			progress = new Progress(this);
			progress.Total = total;
		}

		public void UpdateProgress(long value, long total, string message = null)
		{
			progress.UpdateProgress(value, total, Name);
		}

		ITask ITask.Catch(Action<Exception> handler) => throw new NotImplementedException();

		ITask ITask.Catch(Func<Exception, bool> handler) => throw new NotImplementedException();

		ITask ITask.FinallyInline(Action<bool> handler) => throw new NotImplementedException();

		ITask ITask.Finally(Action<bool, Exception> actionToContinueWith, string name, TaskAffinity affinity) => throw new NotImplementedException();

		T ITask.Finally<T>(T taskToContinueWith) => throw new NotImplementedException();

		ITask ITask.GetEndOfChain() => throw new NotImplementedException();

		ITask ITask.GetTopOfChain(bool onlyCreated) => throw new NotImplementedException();

		bool ITask.IsChainExclusive() => throw new NotImplementedException();

		ITask ITask.Progress(Action<IProgress> progressHandler) => throw new NotImplementedException();

		void ITask.RunSynchronously() => throw new NotImplementedException();

		ITask ITask.Start() => throw new NotImplementedException();

		T ITask.Then<T>(T continuation, TaskRunOptions runOptions, bool taskIsTopOfChain) => throw new NotImplementedException();

		public string Message { get; set; }
		public string Name { get; set; }

		bool ITask.Successful => throw new NotImplementedException();

		string ITask.Errors => throw new NotImplementedException();

		Task ITask.Task => throw new NotImplementedException();

		TaskAffinity ITask.Affinity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		CancellationToken ITask.Token => throw new NotImplementedException();

		ITaskManager ITask.TaskManager => throw new NotImplementedException();

		TaskBase ITask.DependsOn => throw new NotImplementedException();

		Exception ITask.Exception => throw new NotImplementedException();

		object IAsyncResult.AsyncState => throw new NotImplementedException();

		WaitHandle IAsyncResult.AsyncWaitHandle => throw new NotImplementedException();

		bool IAsyncResult.CompletedSynchronously => throw new NotImplementedException();

		bool IAsyncResult.IsCompleted => throw new NotImplementedException();
	}

	public class TaskBase : ITask
	{
		protected const TaskContinuationOptions runAlwaysOptions = TaskContinuationOptions.None;
		protected const TaskContinuationOptions runOnSuccessOptions = TaskContinuationOptions.OnlyOnRanToCompletion;
		protected const TaskContinuationOptions runOnFaultOptions = TaskContinuationOptions.OnlyOnFaulted;
		public static ITask Default = new TaskBase { Name = "Global", Task = TaskHelpers.GetCompletedTask() };
		protected TaskBase continuationOnAlways;
		protected TaskBase continuationOnFailure;

		protected TaskBase continuationOnSuccess;
		protected bool exceptionWasHandled = false;
		protected bool hasRun = false;
		private ILogging logger;
		protected Exception previousException;

		protected bool? previousSuccess;

		protected Progress progress;
		protected bool taskFailed = false;
		public event Action<ITask, bool, Exception> OnEnd;

		public event Action<ITask> OnStart;

		protected event Func<Exception, bool> catchHandler;
		private event Action<bool> finallyHandler;

		protected TaskBase() {}

		protected TaskBase(ITaskManager taskManager)
			: this(taskManager, taskManager?.Token ?? default)
		{}

		protected TaskBase(ITaskManager taskManager, CancellationToken token)
		{
			taskManager.EnsureNotNull(nameof(taskManager));

			TaskManager = taskManager;
			Token = token;
			progress = new Progress(this);
			Task = new Task(RunSynchronously, Token, TaskCreationOptions.None);
		}

		public virtual T Then<T>(T nextTask, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false)
			where T : ITask
		{
			Guard.EnsureNotNull(nextTask, nameof(nextTask));
			var nextTaskBase = ((TaskBase)(object)nextTask);

			// find the task at the top of the chain
			if (!taskIsTopOfChain)
				nextTaskBase = nextTaskBase.GetTopMostTask() ?? nextTaskBase;
			// make the next task dependent on this one so it can get values from us
			nextTaskBase.SetDependsOn(this);
			var nextTaskFinallyHandler = nextTaskBase.finallyHandler;

			if (runOptions == TaskRunOptions.OnSuccess)
			{
				continuationOnSuccess = nextTaskBase;

				// if there are fault handlers in the chain we're appending, propagate them
				// up this chain as well
				if (nextTaskBase.continuationOnFailure != null)
					SetFaultHandler(nextTaskBase.continuationOnFailure);
				else if (nextTaskBase.continuationOnAlways != null)
					SetFaultHandler(nextTaskBase.continuationOnAlways);
				if (nextTaskBase.catchHandler != null)
					Catch(nextTaskBase.catchHandler);
				if (nextTaskFinallyHandler != null)
					FinallyInline(nextTaskFinallyHandler);
			}
			else if (runOptions == TaskRunOptions.OnFailure)
			{
				continuationOnFailure = nextTaskBase;
				DependsOn?.Then(nextTaskBase, TaskRunOptions.OnFailure, true);
			}
			else
			{
				continuationOnAlways = nextTaskBase;
				DependsOn?.SetFaultHandler(nextTaskBase);
			}

			// if the current task has a fault handler, attach it to the chain we're appending
			if (finallyHandler != null)
			{
				var endOfChainTask = (TaskBase)nextTaskBase.GetEndOfChain();
				while (endOfChainTask != this && endOfChainTask != null)
				{
					endOfChainTask.finallyHandler += finallyHandler;
					endOfChainTask = endOfChainTask.DependsOn;
				}
			}

			return nextTask;
		}

		/// <summary>
		/// Catch runs right when the exception happens (on the same thread)
		/// Chain will be cancelled
		/// </summary>
		public ITask Catch(Action<Exception> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			catchHandler += e => { handler(e); return false; };
			DependsOn?.Catch(handler);
			return this;
		}

		/// <summary>
		/// Catch runs right when the exception happens (on the same threaD)
		/// Return true if you want the task to completely successfully
		/// </summary>
		public ITask Catch(Func<Exception, bool> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			CatchInternal(handler);
			DependsOn?.Catch(handler);
			return this;
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
		/// This will always run on the same thread as the previous task
		/// </summary>
		public ITask FinallyInline(Action<bool> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			finallyHandler += handler;
			DependsOn?.FinallyInline(handler);
			return this;
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		public ITask Finally(Action<bool, Exception> actionToContinueWith, string name = null, TaskAffinity affinity = TaskAffinity.ThreadPool)
		{
			Guard.EnsureNotNull(actionToContinueWith, nameof(actionToContinueWith));

			var finallyTask = new ActionTask(TaskManager, Token, (s, ex) => {
				actionToContinueWith(s, ex);
				if (!s)
					ex.Rethrow();
			}) { Affinity = affinity, Name = name ?? (affinity == TaskAffinity.UI ? "FinallyInUI" : "Finally") };

			return Then(finallyTask, TaskRunOptions.OnAlways)
				.CatchInternal(_ => true);
		}

		/// <summary>
		/// Run another task at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		public T Finally<T>(T taskToContinueWith)
			where T : ITask
		{
			return Then(taskToContinueWith, TaskRunOptions.OnAlways);
		}

		/// <summary>
		/// Progress provides progress reporting from the task (on the same thread)
		/// </summary>
		public ITask Progress(Action<IProgress> handler)
		{
			Guard.EnsureNotNull(handler, nameof(handler));
			progress.OnProgress += handler;
			return this;
		}

		public ITask Start()
		{
			var depends = GetTopMostStartableTask();
			depends?.Schedule();
			return this;
		}

		public virtual void RunSynchronously()
		{
			RaiseOnStart();
			Token.ThrowIfCancellationRequested();
			var previousIsSuccessful = previousSuccess ?? (DependsOn?.Successful ?? true);
			try
			{
				Run(previousIsSuccessful);
			}
			finally
			{
				RaiseOnEnd();
			}
		}

		internal void Start(TaskScheduler scheduler)
		{
			if (Task.Status == TaskStatus.Created)
			{
				Task.Start(scheduler);
			}
		}

		public ITask GetTopOfChain(bool onlyCreated = true)
		{
			return GetTopMostTask(null, onlyCreated, false);
		}

		public ITask GetEndOfChain()
		{
			if (continuationOnSuccess != null)
				return continuationOnSuccess.GetEndOfChain();
			else if (continuationOnAlways != null)
				return continuationOnAlways.GetEndOfChain();
			return this;
		}

		/// <summary>
		/// </summary>
		/// <returns>true if any task on the chain is marked as exclusive</returns>
		public bool IsChainExclusive()
		{
			if (Affinity == TaskAffinity.Exclusive)
				return true;
			return DependsOn?.IsChainExclusive() ?? false;
		}

		public void UpdateProgress(long value, long total, string message = null)
		{
			progress.UpdateProgress(value, total, message ?? Message);
		}

		public override string ToString()
		{
			return $"{Task?.Id ?? -1} {Name} {GetType()}";
		}

		protected virtual void Schedule()
		{
			if (Task.Status == TaskStatus.Created)
			{
				TaskManager.Schedule(this);
			}
		}

		/// <summary>
		/// Call this to run a task after another task is done, without
		/// having them depend on each other
		/// </summary>
		/// <param name="task"></param>
		protected void Start(Task task)
		{
			previousSuccess = task.Status == TaskStatus.RanToCompletion && task.Status != TaskStatus.Faulted;
			previousException = task.Exception;
			Task.Start(TaskManager.GetScheduler(Affinity));
			SetContinuation();
		}

		protected void SetContinuation()
		{
			if (continuationOnAlways != null)
			{
				SetContinuation(continuationOnAlways, runAlwaysOptions);
			}
		}

		protected void SetContinuation(TaskBase continuation, TaskContinuationOptions runOptions)
		{
			Token.ThrowIfCancellationRequested();
			Task.ContinueWith(_ =>
				{
					Token.ThrowIfCancellationRequested();
					((TaskBase)(object)continuation).Schedule();
				},
				Token,
				runOptions,
				TaskManager.GetScheduler(continuation.Affinity));
		}

		protected ITask SetDependsOn(ITask dependsOn)
		{
			DependsOn = (TaskBase)dependsOn;
			return this;
		}

		/// <summary>
		/// Returns the first startable task on the chain. If the chain has been started
		/// already, returns null
		/// </summary>
		protected TaskBase GetTopMostStartableTask()
		{
			return GetTopMostTask(null, true, true);
		}

		protected TaskBase GetTopMostCreatedTask()
		{
			return GetTopMostTask(null, true, false);
		}

		protected TaskBase GetTopMostTask()
		{
			return GetTopMostTask(null, false, false);
		}

		protected TaskBase GetTopMostTask(TaskBase ret, bool onlyCreated, bool onlyUnstartedChain)
		{
			ret = (!onlyCreated || Task.Status == TaskStatus.Created ? this : ret);
			var depends = DependsOn;
			if (depends == null)
			{
				// if we're at the top of the chain and the chain has already been started
				// and we only care about unstarted chains, return null
				if (onlyUnstartedChain && Task.Status != TaskStatus.Created)
					return null;
				return ret;
			}
			return depends.GetTopMostTask(ret, onlyCreated, onlyUnstartedChain);
		}

		protected virtual void Run(bool success)
		{
			taskFailed = false;
			hasRun = false;
			ThrownException = null;
			Token.ThrowIfCancellationRequested();
		}

		protected virtual void RaiseOnStart()
		{
			UpdateProgress(0, 100);
			RaiseOnStartInternal();
		}

		protected void RaiseOnStartInternal()
		{
			Logger.Trace($"OnStart: {Name} [{(TaskManager.InUIThread ? "UI Thread" : Affinity.ToString())}]");
			OnStart?.Invoke(this);
		}

		protected virtual bool RaiseFaultHandlers(Exception ex)
		{
			ThrownException = ex;
			if (ThrownException is AggregateException)
				ThrownException = ThrownException.GetBaseException() ?? ThrownException;
			Errors = ThrownException.Message;
			taskFailed = true;
			if (catchHandler == null)
				return false;
			var args = new object[] { ThrownException };
			foreach (var handler in catchHandler.GetInvocationList())
			{
				if ((bool)handler.DynamicInvoke(args))
				{
					exceptionWasHandled = true;
					break;
				}
			}
			// if a catch handler returned true, don't throw
			return exceptionWasHandled;
		}

		protected virtual void RaiseOnEnd()
		{
			hasRun = true;
			RaiseOnEndInternal();
			SetupContinuations();
			UpdateProgress(100, 100);
		}

		protected void RaiseOnEndInternal()
		{
			Logger.Trace($"OnEnd: {Name} [{(TaskManager.InUIThread ? "UI Thread" : Affinity.ToString())}]");
			OnEnd?.Invoke(this, !taskFailed, ThrownException);
		}

		protected void SetupContinuations()
		{
			if (!taskFailed || exceptionWasHandled)
			{
				var taskToContinueWith = continuationOnSuccess ?? continuationOnAlways;
				if (taskToContinueWith != null)
					SetContinuation(taskToContinueWith, runOnSuccessOptions);
				else
				{ // there are no more tasks to schedule, call a finally handler if it exists
				  // we need to do this only when there are no more continuations
				  // so that the in-thread finally handler is guaranteed to run after any Finally tasks
					CallFinallyHandler();
				}
			}
			else
			{
				var taskToContinueWith = continuationOnFailure ?? continuationOnAlways;
				if (taskToContinueWith != null)
					SetContinuation(taskToContinueWith, runOnFaultOptions);
				else
				{ // there are no more tasks to schedule, call a finally handler if it exists
				  // we need to do this only when there are no more continuations
				  // so that the in-thread finally handler is guaranteed to run after any Finally tasks
					CallFinallyHandler();
				}
			}
		}

		protected virtual void CallFinallyHandler()
		{
			finallyHandler?.Invoke(!taskFailed);
		}

		protected Exception GetThrownException()
		{
			var depends = DependsOn;
			while (depends != null)
			{
				if (depends.taskFailed)
					return depends.ThrownException;
				depends = depends.DependsOn;
			}
			return null;
		}

		internal ITask CatchInternal(Func<Exception, bool> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			catchHandler += handler;
			return this;
		}

		/// <summary>
		/// This does not set a dependency between the two tasks. Instead,
		/// the Start method grabs the state of the previous task to pass on
		/// to the next task via previousSuccess and previousException
		/// </summary>
		/// <param name="handler"></param>
		internal void SetFaultHandler(TaskBase handler)
		{
			Task.ContinueWith(t =>
				{
					Token.ThrowIfCancellationRequested();
					//Logger.Trace($"SetFaultHandler: {handler.Name} Start()");
					handler.Start(t);
				},
				Token,
				TaskContinuationOptions.OnlyOnFaulted,
				TaskManager.GetScheduler(handler.Affinity));
			DependsOn?.SetFaultHandler(handler);
		}

		protected Exception ThrownException { get; set; }

		public virtual bool Successful => hasRun && !taskFailed;
		public bool IsCompleted => hasRun;
		public Exception Exception => ThrownException ?? GetThrownException();

		public string Errors { get; protected set; }
		public Task Task { get; protected set; }
		public WaitHandle AsyncWaitHandle => (Task as IAsyncResult).AsyncWaitHandle;
		public object AsyncState => (Task as IAsyncResult).AsyncState;
		public bool CompletedSynchronously => (Task as IAsyncResult).CompletedSynchronously;
		public string Name { get; set; }
		public virtual TaskAffinity Affinity { get; set; }
		protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
		public TaskBase DependsOn { get; private set; }
		public CancellationToken Token { get; }
		public ITaskManager TaskManager { get; }
		public virtual string Message { get; set; }
	}

	public class TaskBase<TResult> : TaskBase, ITask<TResult>
	{
		public new static ITask<TResult> Default = new TaskBase<TResult> { Name = "Global", Task = TaskHelpers.GetCompletedTask(default(TResult)) };
		private TResult result;
		public new event Action<ITask<TResult>, TResult, bool, Exception> OnEnd;

		public new event Action<ITask<TResult>> OnStart;

		private event Action<bool, TResult> finallyHandler;

		protected TaskBase() {}
		protected TaskBase(ITaskManager taskManager) : this(taskManager, taskManager?.Token ?? default) {}

		protected TaskBase(ITaskManager taskManager, CancellationToken token)
			: base(taskManager, token)
		{
			Task = new Task<TResult>(RunSynchronously, Token, TaskCreationOptions.None);
		}

		public override T Then<T>(T continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false)
		{
			var nextTask = base.Then<T>(continuation, runOptions, taskIsTopOfChain);
			var nextTaskBase = ((TaskBase)(object)nextTask);
			// if the current task has a fault handler that matches this signature, attach it to the chain we're appending
			if (finallyHandler != null)
			{
				var endOfChainTask = (TaskBase)nextTaskBase.GetEndOfChain();
				while (endOfChainTask != this && endOfChainTask != null)
				{
					if (endOfChainTask is TaskBase<TResult> taskBase)
						taskBase.finallyHandler += finallyHandler;
					endOfChainTask = endOfChainTask.DependsOn;
				}
			}
			return nextTask;
		}

		/// <summary>
		/// Catch runs right when the exception happens (on the same threaD)
		/// Marks the catch as handled so other Catch statements down the chain
		/// won't be called for this exception (but the chain will be cancelled)
		/// </summary>
		public new ITask<TResult> Catch(Action<Exception> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			catchHandler += e => { handler(e); return false; };
			DependsOn?.Catch(handler);
			return this;
		}

		/// <summary>
		/// Catch runs right when the exception happens (on the same thread)
		/// Return false if you want other Catch statements on the chain to also
		/// get called for this exception
		/// </summary>
		public new ITask<TResult> Catch(Func<Exception, bool> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			CatchInternal(handler);
			DependsOn?.Catch(handler);
			return this;
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
		/// This will always run on the same thread as the last task that runs
		/// </summary>
		public ITask<TResult> FinallyInline(Action<bool, TResult> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			finallyHandler += handler;
			DependsOn?.FinallyInline(success => handler(success, default(TResult)));
			return this;
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		public ITask<TResult> Finally(Func<bool, Exception, TResult, TResult> continuation, string name = null, TaskAffinity affinity = TaskAffinity.ThreadPool)
		{
			Guard.EnsureNotNull(continuation, "continuation");

			return Then(new FuncTask<TResult, TResult>(TaskManager, Token, continuation) { Affinity = affinity, Name = name ?? "Finally" }, TaskRunOptions.OnAlways);
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		public ITask Finally(Action<bool, Exception, TResult> continuation, string name = null, TaskAffinity affinity = TaskAffinity.ThreadPool)
		{
			Guard.EnsureNotNull(continuation, "continuation");

			var finallyTask = new ActionTask<TResult>(TaskManager, Token, (s, ex, res) => {
				continuation(s, ex, res);
				if (!s)
					ex.Rethrow();
			}) { Affinity = affinity, Name = name ?? (affinity == TaskAffinity.UI ? "FinallyInUI" : "Finally") };

			return Then(finallyTask, TaskRunOptions.OnAlways)
				.CatchInternal(_ => true);
		}

		public new ITask<TResult> Start()
		{
			base.Start();
			return this;
		}

		/// <summary>
		/// Progress provides progress reporting from the task (on the same thread)
		/// </summary>
		public new ITask<TResult> Progress(Action<IProgress> handler)
		{
			base.Progress(handler);
			return this;
		}

		public new virtual TResult RunSynchronously()
		{
			RaiseOnStart();
			Token.ThrowIfCancellationRequested();
			var previousIsSuccessful = previousSuccess ?? (DependsOn?.Successful ?? true);
			TResult ret = default;
			try
			{
				ret = RunWithReturn(previousIsSuccessful);
			}
			finally
			{
				RaiseOnEnd(ret);
			}
			return ret;
		}

		protected virtual TResult RunWithReturn(bool success)
		{
			base.Run(success);
			return result;
		}

		protected override void RaiseOnStart()
		{
			UpdateProgress(0, 100);
			OnStart?.Invoke(this);
			RaiseOnStartInternal();
		}

		protected virtual void RaiseOnEnd(TResult data)
		{
			result = data;
			hasRun = true;
			OnEnd?.Invoke(this, result, !taskFailed, ThrownException);
			RaiseOnEndInternal();
			SetupContinuations();
			UpdateProgress(100, 100);
		}

		protected override void CallFinallyHandler()
		{
			finallyHandler?.Invoke(!taskFailed, result);
			base.CallFinallyHandler();
		}

		public new Task<TResult> Task
		{
			get => base.Task as Task<TResult>;
			set => base.Task = value;
		}

		public TResult Result => result;
	}

	public abstract class TaskBase<T, TResult> : TaskBase<TResult>
	{
		private readonly Func<T> getPreviousResult;

		protected TaskBase() {}

		protected TaskBase(ITaskManager taskManager, Func<T> getPreviousResult = null)
			: this(taskManager, taskManager?.Token ?? default, getPreviousResult)
		{}

		protected TaskBase(ITaskManager taskManager, CancellationToken token, Func<T> getPreviousResult = null)
			: base(taskManager, token)
		{
			Task = new Task<TResult>(RunSynchronously, Token, TaskCreationOptions.None);
			this.getPreviousResult = getPreviousResult;
		}

		public override TResult RunSynchronously()
		{
			RaiseOnStart();
			Token.ThrowIfCancellationRequested();
			var previousIsSuccessful = previousSuccess ?? (DependsOn?.Successful ?? true);

			// if this task depends on another task and the dependent task was successful, use the value of that other task as input to this task
			// otherwise if there's a method to retrieve the value, call that
			// otherwise use the PreviousResult property
			T prevResult = PreviousResult;
			if (previousIsSuccessful && DependsOn != null && DependsOn is ITask<T>)
				prevResult = ((ITask<T>)DependsOn).Result;
			else if (getPreviousResult != null)
				prevResult = getPreviousResult();

			TResult ret = default;
			try
			{
				ret = RunWithData(previousIsSuccessful, prevResult);
			}
			finally
			{
				RaiseOnEnd(ret);
			}
			return ret;
		}

		protected virtual TResult RunWithData(bool success, T previousResult)
		{
			base.Run(success);
			return default;
		}

		public T PreviousResult { get; set; } = default(T);
	}

	public abstract class DataTaskBase<TData, TResult> : TaskBase<TResult>, ITask<TData, TResult>
	{
		public event Action<TData> OnData;
		protected DataTaskBase() {}

		protected DataTaskBase(ITaskManager taskManager) : base(taskManager) {}
		protected DataTaskBase(ITaskManager taskManager, CancellationToken token) : base(taskManager, token) {}

		protected void RaiseOnData(TData data)
		{
			OnData?.Invoke(data);
		}
	}

	public abstract class DataTaskBase<T, TData, TResult> : TaskBase<T, TResult>, ITask<TData, TResult>
	{
		public event Action<TData> OnData;
		protected DataTaskBase() {}
		protected DataTaskBase(ITaskManager taskManager) : base(taskManager) {}
		protected DataTaskBase(ITaskManager taskManager, CancellationToken token) : base(taskManager, token) {}

		protected void RaiseOnData(TData data)
		{
			OnData?.Invoke(data);
		}
	}
}
