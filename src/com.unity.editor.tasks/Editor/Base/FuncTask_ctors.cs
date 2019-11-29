namespace Unity.Editor.Tasks
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using Helpers;

	public partial class FuncTask<T> : TaskBase<T>
	{
		/// <summary>
		/// Creates an instance of FuncTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncTask(ITaskManager taskManager, Func<T> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{}

		/// <summary>
		/// Creates an instance of FuncTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncTask(ITaskManager taskManager, Func<bool, T> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{}

		/// <summary>
		/// Creates an instance of FuncTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncTask(ITaskManager taskManager, Func<bool, Exception, T> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{}

	}

	public partial class FuncTask<T, TResult> : TaskBase<T, TResult>
	{
		/// <summary>
		/// Creates an instance of FuncTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult"></param>
		public FuncTask(ITaskManager taskManager, Func<T, TResult> action, Func<T> getPreviousResult = null)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, (_, t) => action(t), getPreviousResult)
		{}

		/// <summary>
		/// Creates an instance of FuncTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult"></param>
		public FuncTask(ITaskManager taskManager, CancellationToken token, Func<T, TResult> action, Func<T> getPreviousResult = null)
			: this(taskManager, token, (_, t) => action(t), getPreviousResult)
		{}

		/// <summary>
		/// Creates an instance of FuncTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult"></param>
		public FuncTask(ITaskManager taskManager, Func<bool, T, TResult> action, Func<T> getPreviousResult = null)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action, getPreviousResult)
		{}

		/// <summary>
		/// Creates an instance of FuncTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult"></param>
		public FuncTask(ITaskManager taskManager, Func<bool, Exception, T, TResult> action, Func<T> getPreviousResult = null)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action, getPreviousResult)
		{}

	}

	public partial class FuncListTask<T> : DataTaskBase<T, List<T>>
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<List<T>> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, (_) => action())
		{ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, CancellationToken token, Func<List<T>> action)
			: this(taskManager, token, (_) => action())
		{ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<bool, List<T>> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<bool, Exception, List<T>> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<bool, FuncListTask<T>, List<T>> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{ }

	}

	public partial class FuncListTask<T, TData, TResult> : DataTaskBase<T, TData, List<TResult>>
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<T, List<TResult>> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, (_, t) => action(t))
		{ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, CancellationToken token, Func<T, List<TResult>> action)
			: this(taskManager, token, (_, t) => action(t))
		{ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<bool, T, List<TResult>> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<bool, Exception, T, List<TResult>> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{ }
	}
}
