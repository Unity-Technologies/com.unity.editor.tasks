// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using Logging;
	using Helpers;
	public static class TaskExtensions
	{
		public static async Task StartAwait(this ITask source, Action<Exception> handler = null)
		{
			try
			{
				await source.StartAsAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (handler == null)
					throw;
				handler(ex);
			}
		}

		public static async Task<T> StartAwait<T>(this ITask<T> source, Func<Exception, T> handler = null)
		{
			try
			{
				return await source.StartAsAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (handler == null)
					throw;
				return handler(ex);
			}
		}

		public static void FireAndForget(this Task _)
		{ }

		public static ITask Then(this ITask task, Action continuation, string name = "Then", TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new ActionTask(task.TaskManager, task.Token, _ => continuation()) { Affinity = affinity, Name = name }, runOptions);
		}

		public static ITask Then(this ITask task, Action<bool> continuation, string name = "Then<bool>", TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new ActionTask(task.TaskManager, task.Token, continuation) { Affinity = affinity, Name = name }, runOptions);
		}

		public static ITask Then<T>(this ITask<T> task, Action<bool, T> continuation, string name = null, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new ActionTask<T>(task.TaskManager, task.Token, continuation) { Affinity = affinity, Name = name ?? $"Then<{typeof(T)}>" }, runOptions);
		}

		public static ITask<T> Then<T>(this ITask task, Func<bool, T> continuation, string name = null, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new FuncTask<T>(task.TaskManager, task.Token, continuation) { Affinity = affinity, Name = name ?? $"ThenFunc<{typeof(T)}>" }, runOptions);
		}

		public static ITask<TRet> Then<T, TRet>(this ITask<T> task, Func<bool, T, TRet> continuation, string name = null, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new FuncTask<T, TRet>(task.TaskManager, task.Token, continuation) { Affinity = affinity, Name = name ?? $"ThenFunc<{typeof(T)}, {typeof(TRet)}>" }, runOptions);
		}

		public static ITask<T> Then<T>(this ITask task, Func<Task<T>> continuation, string name = null, TaskAffinity affinity = TaskAffinity.Concurrent, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			var cont = new TPLTask<T>(task.TaskManager, continuation) { Affinity = affinity, Name = name ?? $"ThenAsync<{typeof(T)}>" };
			return task.Then(cont, runOptions);
		}

		public static ITask ThenInUI(this ITask task, Action continuation, string name = "ThenInUI", TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			return task.Then(continuation, name, TaskAffinity.UI, runOptions);
		}

		public static ITask ThenInUI(this ITask task, Action<bool> continuation, string name = "ThenInUI<bool>", TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			return task.Then(continuation, name, TaskAffinity.UI, runOptions);
		}

		public static ITask ThenInUI<T>(this ITask<T> task, Action<bool, T> continuation, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			return task.Then(continuation, name ?? $"ThenInUI<{typeof(T)}>", TaskAffinity.UI, runOptions);
		}

		public static ITask<TRet> ThenInUI<T, TRet>(this ITask<T> task, Func<bool, T, TRet> continuation, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			return task.Then(continuation, name ?? $"ThenInUIFunc<{typeof(T)}, {typeof(TRet)}>", TaskAffinity.UI, runOptions);
		}

		public static ITask FinallyInUI<T>(this T task, Action<bool, Exception> continuation, string name = null)
			where T : ITask
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));

			return task.Finally(continuation, name, TaskAffinity.UI);
		}

		public static ITask FinallyInUI<T>(this ITask<T> task, Action<bool, Exception, T> continuation, string name = null)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));

			return task.Finally(continuation, name, TaskAffinity.UI);
		}

		public static Task<T> StartAsAsync<T>(this ITask<T> task)
		{
			task.EnsureNotNull(nameof(task));

			var tcs = new TaskCompletionSource<T>();
			task.FinallyInline((success, r) =>
			{
				tcs.TrySetResult(r);
			});
			task.Catch(e =>
			{
				tcs.TrySetException(e);
			});
			task.Start();
			return tcs.Task;
		}

		public static Task<bool> StartAsAsync(this ITask task)
		{
			task.EnsureNotNull(nameof(task));

			var tcs = new TaskCompletionSource<bool>();
			task.FinallyInline(success =>
			{
				tcs.TrySetResult(success);
			});
			task.Catch(e =>
			{
				tcs.TrySetException(e);
			});
			task.Start();
			return tcs.Task;
		}
	}
}
