using NUnit.Framework;

namespace ThreadingTests
{
	using System.Threading.Tasks;

	// This file is only compiled when building the solution
	// outside of Unity.
	// Unity does not support async/await tests, but it does
	// have a special type of test with a [CustomUnityTest] attribute
	// which mimicks a coroutine in EditMode. This attribute is
	// defined for tests running outside of Unity, and those tests
	// are executed the await RunTest calls below, if running outside
	// of Unity, so I don't have to keep two copies of tests.
	//
	// Tests that Unity can't run or shouldn't run can be added directly
	// here.

	partial class Chains
	{
		[Test]
		public async Task CatchAlwaysRunsBeforeFinally_()
		{
			await RunTest(CatchAlwaysRunsBeforeFinally);
		}

		[Test]
		public async Task FinallyCanAlsoNotReturnData_()
		{
			await RunTest(FinallyCanAlsoNotReturnData);
		}

		[Test]
		public async Task FinallyCanReturnData_()
		{
			await RunTest(FinallyCanReturnData);
		}

		[Test]
		public async Task FinallyReportsException_()
		{
			await RunTest(FinallyReportsException);
		}

		[Test]
		public async Task ThrowingInterruptsTaskChainButAlwaysRunsFinallyAndCatch_()
		{
			await RunTest(ThrowingInterruptsTaskChainButAlwaysRunsFinallyAndCatch);
		}

		[Test]
		public async Task YouCanUseCatchAtTheEndOfAChain_()
		{
			await RunTest(YouCanUseCatchAtTheEndOfAChain);
		}
	}
}
