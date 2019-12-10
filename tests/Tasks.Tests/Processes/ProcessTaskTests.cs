using System.Threading.Tasks;
using NUnit.Framework;

namespace ProcessManagerTests
{
	using NSubstitute.Extensions;
	using NUnit.Framework.Internal;
	using System;
	using System.Diagnostics;
	using Unity.Editor.Tasks;

	public partial class ProcessTaskTests
	{
		[Test]
		public async Task CanRunProcess_()
		{
			await RunTest(CanRunProcess);
		}

		[Test]
		public async Task CanDetach_()
		{
			Process process = default;
			using (var test = StartTest())
			{
				using (var task = new HelperProcessTask(test.TaskManager, test.ProcessManager,
					TestApp, @"--sleep 1000 --data ""ok"""))
				{

					task.OnStartProcess += p => {
						process = ((ProcessWrapper)p.Wrapper).Process;
						task.Detach();
					};
					var ret = await task.StartAwait();
					Assert.Null(ret);
					Assert.False(task.Wrapper.HasExited);
					Assert.False(process.HasExited);
				}
			}
		}

		[Test]
		public async Task NestedProcessShouldChainCorrectly_()
		{
			await RunTest(NestedProcessShouldChainCorrectly);
		}

		[Test]
		public async Task MultipleFinallyOrder_()
		{
			await RunTest(MultipleFinallyOrder);
		}

		[Test]
		public async Task ProcessOnStartOnEndTaskOrder_()
		{
			await RunTest(ProcessOnStartOnEndTaskOrder);
		}

		[Test]
		public async Task ProcessReadsFromStandardInput_()
		{
			await RunTest(ProcessReadsFromStandardInput);
		}

		[Test]
		public async Task ProcessReturningErrorThrowsException_()
		{
			await RunTest(ProcessReturningErrorThrowsException);
		}

		[Test]
		public async Task RunProcessOnMono_()
		{
			await RunTest(CanRunProcessOnMono);
		}
	}
}
