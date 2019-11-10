// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	[Serializable]
	public class DependentTaskFailedException : TaskCanceledException
	{
		protected DependentTaskFailedException() : base()
		{ }

		protected DependentTaskFailedException(string message) : base(message)
		{ }

		protected DependentTaskFailedException(string message, Exception innerException) : base(message, innerException)
		{ }

		protected DependentTaskFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }

		public DependentTaskFailedException(ITask task, Exception ex) : this(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex.InnerException ?? ex)
		{}
	}

	[Serializable]
	public class ProcessException : TaskCanceledException
	{
		protected ProcessException() : base()
		{ }

		public ProcessException(int errorCode, string message) : base(message)
		{
			ErrorCode = errorCode;
		}

		public ProcessException(int errorCode, string message, Exception innerException) : base(message, innerException)
		{
			ErrorCode = errorCode;
		}

		public ProcessException(string message) : base(message)
		{ }

		public ProcessException(string message, Exception innerException) : base(message, innerException)
		{ }

		protected ProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }

		public ProcessException(ITask process) : this(process.Errors)
		{ }

		public int ErrorCode { get; }
	}
}
