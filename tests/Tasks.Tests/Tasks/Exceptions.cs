using System.Threading.Tasks;
using NUnit.Framework;

namespace ThreadingTests
{
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

	partial class Exceptions
	{
		[Test]
		public async Task AllFinallyHandlersAreCalledOnException_()
		{
			await RunTest(AllFinallyHandlersAreCalledOnException);
		}

		[Test]
		public async Task ContinueAfterException_()
		{
			await RunTest(ContinueAfterException);
		}

		[Test]
		public async Task ExceptionPropagatesOutIfNoFinally_()
		{
			await RunTest(ExceptionPropagatesOutIfNoFinally);
		}

		[Test]
		public async Task MultipleCatchStatementsCanHappen_()
		{
			await RunTest(MultipleCatchStatementsCanHappen);
		}

		[Test]
		public async Task StartAndEndAreAlwaysRaised_()
		{
			await RunTest(StartAndEndAreAlwaysRaised);
		}

	}
}
