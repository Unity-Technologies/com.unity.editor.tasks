using NUnit.Framework;

namespace ThreadingTests
{
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
		public void CatchAlwaysRunsBeforeFinally_()
		{
			RunTest(CatchAlwaysRunsBeforeFinally);
		}

		[Test]
		public void FinallyCanAlsoNotReturnData_()
		{
			RunTest(FinallyCanAlsoNotReturnData);
		}

		[Test]
		public void FinallyCanReturnData_()
		{
			RunTest(FinallyCanReturnData);
		}

		[Test]
		public void FinallyReportsException_()
		{
			RunTest(FinallyReportsException);
		}

		[Test]
		public void ThrowingInterruptsTaskChainButAlwaysRunsFinallyAndCatch_()
		{
			RunTest(ThrowingInterruptsTaskChainButAlwaysRunsFinallyAndCatch);
		}

		[Test]
		public void YouCanUseCatchAtTheEndOfAChain_()
		{
			RunTest(YouCanUseCatchAtTheEndOfAChain);
		}
	}
}
