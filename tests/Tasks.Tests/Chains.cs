using NUnit.Framework;

namespace ThreadingTests
{
	using System.Threading.Tasks;

	// Unity does not support async/await tests, but it does
	// have a special type of test with a [CustomUnityTest] attribute
	// which mimicks a coroutine in EditMode. This attribute is
	// defined here so the tests can be compiled without
	// referencing Unity, and nunit on the command line
	// outside of Unity can execute the tests. Basically I don't
	// want to keep two copies of all the tests so I run the
	// UnityTest from here

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
