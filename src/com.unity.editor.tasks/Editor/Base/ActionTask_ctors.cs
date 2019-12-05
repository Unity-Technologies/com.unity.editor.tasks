// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace Unity.Editor.Tasks
{
	using Helpers;
	using System.Threading;

	public partial class ActionTask
	{
		/// <summary>
		/// Creates an instance of ActionTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, Action action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{ }

		/// <summary>
		/// Creates an instance of ActionTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, Action<bool> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{ }

		/// <summary>
		/// Creates an instance of ActionTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, Action<bool, Exception> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action)
		{ }
	}

	public partial class ActionTask<T> : TaskBase
	{
		/// <summary>
		/// Creates an instance of ActionTask<typeparamref name="T"/>, using the cancellation token set in the task manager.
		/// The delegate of the task will get the value that is returned from a previous task, if any, or from the <see cref="PreviousResult"/> property,
		/// if set.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action">A delegate thar receives a value of type T</param>
		public ActionTask(ITaskManager taskManager, Action<T> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, (_, t) => action(t))
		{ }

		/// <summary>
		/// Creates an instance of ActionTask<typeparamref name="T"/>, using the cancellation token set in the task manager.
		/// The delegate of the task will get the value returned from the <paramref name="getPreviousResult"/> delegate.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action">A delegate thar receives a value of type T</param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with.</param>
		public ActionTask(ITaskManager taskManager, Action<T> action, Func<T> getPreviousResult)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, (_, t) => action(t), getPreviousResult)
		{ }

		/// <summary>
		/// Creates an instance of ActionTask<typeparamref name="T"/>, using the cancellation token provided in <paramref name="token"/>.
		/// The delegate of the task will get the value that is returned from a previous task, if any, or from the <see cref="PreviousResult"/> property,
		/// if set.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token">The cancellation token to listen to.</param>
		/// <param name="action">A delegate thar receives a value of type T</param>
		public ActionTask(ITaskManager taskManager, CancellationToken token, Action<T> action)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, (_, t) => action(t))
		{ }

		/// <summary>
		/// Creates an instance of ActionTask<typeparamref name="T"/>, using the cancellation token provided in <paramref name="token"/>.
		/// The delegate of the task will get the value returned from the <paramref name="getPreviousResult"/> delegate.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token">The cancellation token to listen to.</param>
		/// <param name="action">A delegate thar receives a value of type T</param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with.</param>
		public ActionTask(ITaskManager taskManager, CancellationToken token, Action<T> action, Func<T> getPreviousResult)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, (_, t) => action(t), getPreviousResult)
		{ }

		/// <summary>
		/// Creates an instance of ActionTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action">A delegate thar receives a bool indicating the success or failure of the previous task (if any), and a value of type T</param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
		public ActionTask(ITaskManager taskManager, Action<bool, T> action, Func<T> getPreviousResult = null)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action, getPreviousResult)
		{}

		/// <summary>
		/// Creates an instance of ActionTask, using the cancellation token set in the task manager.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action">A delegate thar receives a bool indicating the success or failure of the previous task (if any), the exception thrown by the previous task (if any), and a value of type T</param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
		public ActionTask(ITaskManager taskManager, Action<bool, Exception, T> action, Func<T> getPreviousResult = null)
			: this(taskManager.EnsureNotNull(nameof(taskManager)), taskManager.Token, action, getPreviousResult)
		{ }

	}
}
