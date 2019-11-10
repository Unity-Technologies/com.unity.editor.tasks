// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	static class TaskHelpers
	{
		public static Task<T> GetCompletedTask<T>(T result)
		{
#if NET35
			return TaskEx.FromResult(result);
#else
			return Task.FromResult(result);
#endif
		}

		public static Task GetCompletedTask()
		{
			return Task.CompletedTask;
		}

		public static Task<T> ToTask<T>(this Exception exception)
		{
			TaskCompletionSource<T> completionSource = new TaskCompletionSource<T>();
			completionSource.TrySetException(exception);
			return completionSource.Task;
		}
	}

	[Serializable]
	public class NotReadyException : Exception
	{
		public NotReadyException() : base()
		{ }

		public NotReadyException(string message) : base(message)
		{ }

		public NotReadyException(string message, Exception innerException) : base(message, innerException)
		{ }

		protected NotReadyException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }
	}
}
