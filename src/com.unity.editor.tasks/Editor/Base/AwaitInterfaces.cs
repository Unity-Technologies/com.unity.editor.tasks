// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Runtime.CompilerServices;

namespace Unity.Editor.Tasks
{
	interface IAwaitable
	{
		IAwaiter GetAwaiter();
	}

	interface IAwaiter : INotifyCompletion
	{
		void GetResult();
		bool IsCompleted { get; }
	}
}
