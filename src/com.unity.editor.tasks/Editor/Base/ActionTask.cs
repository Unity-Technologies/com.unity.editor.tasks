// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using Helpers;

	public class TaskQueue : TPLTask
	{
		private readonly TaskCompletionSource<bool> aggregateTask = new TaskCompletionSource<bool>();
		private readonly List<ITask> queuedTasks = new List<ITask>();
		private int finishedTaskCount;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		public TaskQueue(ITaskManager taskManager) : base(taskManager)
		{
			Initialize(aggregateTask.Task);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		public TaskQueue(ITaskManager taskManager, CancellationToken token) : base(taskManager, token)
		{
			Initialize(aggregateTask.Task);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		public ITask Queue(ITask task)
		{
			// if this task fails, both OnEnd and Catch will be called
			// if a task before this one on the chain fails, only Catch will be called
			// so avoid calling TaskFinished twice by ignoring failed OnEnd calls
			task.OnEnd += InvokeFinishOnlyOnSuccess;
			task.Catch(e => TaskFinished(false, e));
			queuedTasks.Add(task);
			return this;
		}

		public override void RunSynchronously()
		{
			if (queuedTasks.Any())
			{
				foreach (var task in queuedTasks)
					task.Start();
			}
			else
			{
				aggregateTask.TrySetResult(true);
			}

			base.RunSynchronously();
		}

		protected override void Schedule()
		{
			if (queuedTasks.Any())
			{
				foreach (var task in queuedTasks)
					task.Start();
			}
			else
			{
				aggregateTask.TrySetResult(true);
			}

			base.Schedule();
		}

		private void InvokeFinishOnlyOnSuccess(ITask task, bool success, Exception ex)
		{
			if (success)
				TaskFinished(true, null);
		}

		private void TaskFinished(bool success, Exception ex)
		{
			var count = Interlocked.Increment(ref finishedTaskCount);
			if (count == queuedTasks.Count)
			{
				var exceptions = queuedTasks.Where(x => !x.Successful).Select(x => x.Exception).ToArray();
				var isSuccessful = exceptions.Length == 0;

				if (isSuccessful)
				{
					aggregateTask.TrySetResult(true);
				}
				else
				{
					aggregateTask.TrySetException(new AggregateException(exceptions));
				}
			}
		}
	}

	public class TaskQueue<TResult> : TaskQueue<TResult, TResult>
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		public TaskQueue(ITaskManager taskManager) : base(taskManager) {}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		public TaskQueue(ITaskManager taskManager, CancellationToken token) : base(taskManager, token) { }
	}

	public class TaskQueue<TTaskResult, TResult> : TPLTask<List<TResult>>
	{
		private readonly TaskCompletionSource<List<TResult>> aggregateTask = new TaskCompletionSource<List<TResult>>();
		private readonly ProgressReporter progressReporter = new ProgressReporter();
		private readonly List<ITask<TTaskResult>> queuedTasks = new List<ITask<TTaskResult>>();
		private readonly Func<ITask<TTaskResult>, TResult> resultConverter;
		private int finishedTaskCount;

		/// <summary>
		/// If <typeparamref name="TTaskResult"/> is not assignable to <typeparamref name="TResult"/>, you must pass a
		/// method to convert between the two. Implicit conversions don't count (so even though SPath has an implicit
		/// conversion to string, you still need to pass in a converter)
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="resultConverter"></param>
		public TaskQueue(ITaskManager taskManager, Func<ITask<TTaskResult>, TResult> resultConverter = null)
			: this(taskManager, taskManager?.Token ?? default, resultConverter)
		{}

		/// <summary>
		/// If <typeparamref name="TTaskResult"/> is not assignable to <typeparamref name="TResult"/>, you must pass a
		/// method to convert between the two. Implicit conversions don't count (so even though SPath has an implicit
		/// conversion to string, you still need to pass in a converter)
		/// </summary>
		/// <param name="token"></param>
		/// <param name="resultConverter"></param>
		/// <param name="taskManager"></param>
		public TaskQueue(ITaskManager taskManager, CancellationToken token, Func<ITask<TTaskResult>, TResult> resultConverter = null)
			: base(taskManager, token)
		{
			// this excludes implicit operators - that requires using reflection to figure out if
			// the types are convertible, and I'd rather not do that
			if (resultConverter == null && !typeof(TResult).IsAssignableFrom(typeof(TTaskResult)))
			{
				throw new ArgumentNullException(nameof(resultConverter),
					String.Format(CultureInfo.InvariantCulture, "Cannot cast {0} to {1} and no {2} method was passed in to do the conversion", typeof(TTaskResult), typeof(TResult), nameof(resultConverter)));
			}
			this.resultConverter = resultConverter;
			Initialize(aggregateTask.Task);
			progressReporter.OnProgress += progress.UpdateProgress;
		}

		/// <summary>
		/// Queues an ITask for running, and when the task is done, <paramref name="theResultConverter"/> is called
		/// to convert the result of the task to something else
		/// </summary>
		/// <param name="task"></param>
		///
		/// <returns></returns>
		public ITask<TTaskResult> Queue(ITask<TTaskResult> task)
		{
			progressReporter.Message = Message;

			// if this task fails, both OnEnd and Catch will be called
			// if a task before this one on the chain fails, only Catch will be called
			// so avoid calling TaskFinished twice by ignoring failed OnEnd calls
			task.Progress(progressReporter.UpdateProgress);
			task.OnEnd += InvokeFinishOnlyOnSuccess;
			task.Catch(e => TaskFinished(default, false, e));
			queuedTasks.Add(task);
			return task;
		}

		public override List<TResult> RunSynchronously()
		{
			if (queuedTasks.Any())
			{
				foreach (var task in queuedTasks)
					task.Start();
			}
			else
			{
				aggregateTask.TrySetResult(new List<TResult>());
			}

			return base.RunSynchronously();
		}

		protected override void Schedule()
		{
			if (queuedTasks.Any())
			{
				foreach (var task in queuedTasks)
					task.Start();
			}
			else
			{
				aggregateTask.TrySetResult(new List<TResult>());
			}

			base.Schedule();
		}

		private void InvokeFinishOnlyOnSuccess(ITask<TTaskResult> task, TTaskResult result, bool success, Exception ex)
		{
			if (success)
				TaskFinished(result, true, null);
		}

		private void TaskFinished(TTaskResult result, bool success, Exception ex)
		{
			var count = Interlocked.Increment(ref finishedTaskCount);
			if (count == queuedTasks.Count)
			{
				var exceptions = queuedTasks.Where(x => !x.Successful).Select(x => x.Exception).ToArray();
				var isSuccessful = exceptions.Length == 0;

				if (isSuccessful)
				{
					List<TResult> results;
					if (resultConverter != null)
						results = queuedTasks.Select(x => resultConverter(x)).ToList();
					else
						results = queuedTasks.Select(x => (TResult)(object)x.Result).ToList();
					aggregateTask.TrySetResult(results);
				}
				else
				{
					aggregateTask.TrySetException(new AggregateException(exceptions));
				}
			}
		}
	}
	
	public partial class ActionTask : TaskBase
	{
		protected ActionTask() {}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, Action action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, CancellationToken token, Action action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			Callback = _ => action();
			Name = "ActionTask";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, Action<bool> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, CancellationToken token, Action<bool> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			Callback = action;
			Name = "ActionTask";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, Action<bool, Exception> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, CancellationToken token, Action<bool, Exception> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			CallbackWithException = action;
			Name = "ActionTask<Exception>";
		}

		protected override void Run(bool success)
		{
			base.Run(success);
			try
			{
				Callback?.Invoke(success);
				if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					CallbackWithException?.Invoke(success, thrown);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					ThrownException.Rethrow();
			}
		}

		protected Action<bool> Callback { get; }
		protected Action<bool, Exception> CallbackWithException { get; }
	}

	public partial class ActionTask<T> : TaskBase
	{
		private readonly Func<T> getPreviousResult;

		protected ActionTask() {}

		/// <summary>
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
		public ActionTask(ITaskManager taskManager, Action<bool, T> action, Func<T> getPreviousResult = null)
			: this(taskManager, taskManager?.Token ?? default, action, getPreviousResult)
		{}

		/// <summary>
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
		public ActionTask(ITaskManager taskManager, CancellationToken token, Action<bool, T> action, Func<T> getPreviousResult = null)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");

			this.getPreviousResult = getPreviousResult;
			Callback = action;
			Task = new Task(RunSynchronously, Token, TaskCreationOptions.None);
			Name = $"ActionTask<{typeof(T)}>";
		}

		/// <summary>
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
		public ActionTask(ITaskManager taskManager, Action<bool, Exception, T> action, Func<T> getPreviousResult = null)
			: this(taskManager, taskManager?.Token ?? default, action, getPreviousResult)
		{ }

		/// <summary>
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
		public ActionTask(ITaskManager taskManager, CancellationToken token, Action<bool, Exception, T> action, Func<T> getPreviousResult = null)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");

			this.getPreviousResult = getPreviousResult;
			CallbackWithException = action;
			Task = new Task(RunSynchronously, Token, TaskCreationOptions.None);
			Name = $"ActionTask<Exception, {typeof(T)}>";
		}

		public override void RunSynchronously()
		{
			RaiseOnStart();
			Token.ThrowIfCancellationRequested();
			var previousIsSuccessful = previousSuccess.HasValue ? previousSuccess.Value : (DependsOn?.Successful ?? true);

			// if this task depends on another task and the dependent task was successful, use the value of that other task as input to this task
			// otherwise if there's a method to retrieve the value, call that
			// otherwise use the PreviousResult property
			T prevResult = PreviousResult;
			if (previousIsSuccessful && DependsOn != null && DependsOn is ITask<T>)
				prevResult = ((ITask<T>)DependsOn).Result;
			else if (getPreviousResult != null)
				prevResult = getPreviousResult();

			try
			{
				Run(previousIsSuccessful, prevResult);
			}
			finally
			{
				RaiseOnEnd();
			}
		}

		protected virtual void Run(bool success, T previousResult)
		{
			base.Run(success);
			try
			{
				Callback?.Invoke(success, previousResult);
				if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					CallbackWithException?.Invoke(success, thrown, previousResult);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					ThrownException.Rethrow();
			}
		}

		protected Action<bool, T> Callback { get; }
		protected Action<bool, Exception, T> CallbackWithException { get; }

		public T PreviousResult { get; set; } = default(T);
	}

	public partial class FuncTask<T> : TaskBase<T>
	{
		protected FuncTask() {}

		public FuncTask(ITaskManager taskManager, Func<T> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		public FuncTask(ITaskManager taskManager, CancellationToken token, Func<T> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			Callback = _ => action();
			Name = $"FuncTask<{typeof(T)}>";
		}

		public FuncTask(ITaskManager taskManager, Func<bool, T> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		public FuncTask(ITaskManager taskManager, CancellationToken token, Func<bool, T> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			Callback = action;
			Name = $"FuncTask<{typeof(T)}>";
		}

		public FuncTask(ITaskManager taskManager, Func<bool, Exception, T> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		public FuncTask(ITaskManager taskManager, CancellationToken token, Func<bool, Exception, T> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			CallbackWithException = action;
			Name = $"FuncTask<Exception, {typeof(T)}>";
		}

		protected override T RunWithReturn(bool success)
		{
			T result = base.RunWithReturn(success);
			try
			{
				if (Callback != null)
				{
					result = Callback(success);
				}
				else if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					result = CallbackWithException(success, thrown);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					ThrownException.Rethrow();
			}
			return result;
		}

		protected Func<bool, T> Callback { get; }
		protected Func<bool, Exception, T> CallbackWithException { get; }
	}

	public partial class FuncTask<T, TResult> : TaskBase<T, TResult>
	{
		protected FuncTask() {}

		public FuncTask(ITaskManager taskManager, Func<bool, T, TResult> action, Func<T> getPreviousResult = null)
			: this(taskManager, taskManager?.Token ?? default, action, getPreviousResult)
		{}

		public FuncTask(ITaskManager taskManager, CancellationToken token, Func<bool, T, TResult> action, Func<T> getPreviousResult = null)
			: base(taskManager, token, getPreviousResult)
		{
			Guard.ArgumentNotNull(action, "action");
			Callback = action;
			Name = $"FuncTask<{typeof(T)}, {typeof(TResult)}>";
		}

		public FuncTask(ITaskManager taskManager, Func<bool, Exception, T, TResult> action, Func<T> getPreviousResult = null)
			: this(taskManager, taskManager?.Token ?? default, action, getPreviousResult)
		{}

		public FuncTask(ITaskManager taskManager, CancellationToken token, Func<bool, Exception, T, TResult> action, Func<T> getPreviousResult = null)
			: base(taskManager, token, getPreviousResult)
		{
			Guard.ArgumentNotNull(action, "action");
			CallbackWithException = action;
			Name = $"FuncTask<{typeof(T)}, Exception, {typeof(TResult)}>";
		}

		protected override TResult RunWithData(bool success, T previousResult)
		{
			var result = base.RunWithData(success, previousResult);
			try
			{
				if (Callback != null)
				{
					result = Callback(success, previousResult);
				}
				else if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					result = CallbackWithException(success, thrown, previousResult);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					ThrownException.Rethrow();
			}
			return result;
		}

		protected Func<bool, T, TResult> Callback { get; }
		protected Func<bool, Exception, T, TResult> CallbackWithException { get; }
	}

	public partial class FuncListTask<T> : DataTaskBase<T, List<T>>
	{
		protected FuncListTask() {}

		public FuncListTask(ITaskManager taskManager, Func<bool, List<T>> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		public FuncListTask(ITaskManager taskManager, CancellationToken token, Func<bool, List<T>> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			Callback = action;
		}

		public FuncListTask(ITaskManager taskManager, Func<bool, Exception, List<T>> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		public FuncListTask(ITaskManager taskManager, CancellationToken token, Func<bool, Exception, List<T>> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			CallbackWithException = action;
		}

		public FuncListTask(ITaskManager taskManager, Func<bool, FuncListTask<T>, List<T>> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		public FuncListTask(ITaskManager taskManager, CancellationToken token, Func<bool, FuncListTask<T>, List<T>> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			CallbackWithSelf = action;
		}

		protected override List<T> RunWithReturn(bool success)
		{
			var result = base.RunWithReturn(success);
			try
			{
				if (Callback != null)
				{
					result = Callback(success);
				}
				else if (CallbackWithSelf != null)
				{
					result = CallbackWithSelf(success, this);
				}
				else if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					result = CallbackWithException(success, thrown);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					ThrownException.Rethrow();
			}
			finally
			{
				if (result == null)
					result = new List<T>();
			}
			return result;
		}

		protected Func<bool, List<T>> Callback { get; }
		protected Func<bool, FuncListTask<T>, List<T>> CallbackWithSelf { get; }
		protected Func<bool, Exception, List<T>> CallbackWithException { get; }
	}

	public partial class FuncListTask<T, TData, TResult> : DataTaskBase<T, TData, List<TResult>>
	{
		protected FuncListTask() {}

		public FuncListTask(ITaskManager taskManager, Func<bool, T, List<TResult>> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		public FuncListTask(ITaskManager taskManager, CancellationToken token, Func<bool, T, List<TResult>> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			Callback = action;
		}

		public FuncListTask(ITaskManager taskManager, Func<bool, Exception, T, List<TResult>> action)
			: this(taskManager, taskManager?.Token ?? default, action)
		{}

		public FuncListTask(ITaskManager taskManager, CancellationToken token, Func<bool, Exception, T, List<TResult>> action)
			: base(taskManager, token)
		{
			Guard.ArgumentNotNull(action, "action");
			CallbackWithException = action;
		}

		protected override List<TResult> RunWithData(bool success, T previousResult)
		{
			var result = base.RunWithData(success, previousResult);
			try
			{
				if (Callback != null)
				{
					result = Callback(success, previousResult);
				}
				else if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					result = CallbackWithException(success, thrown, previousResult);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					ThrownException.Rethrow();
			}
			return result;
		}

		protected Func<bool, T, List<TResult>> Callback { get; }
		protected Func<bool, Exception, T, List<TResult>> CallbackWithException { get; }
	}
}
