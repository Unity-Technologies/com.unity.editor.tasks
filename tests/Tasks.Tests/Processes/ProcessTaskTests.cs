using System.Threading.Tasks;
using NUnit.Framework;

namespace ProcessManagerTests
{
	using NUnit.Framework.Internal;

	public partial class ProcessTaskTests
	{

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
	}
}
