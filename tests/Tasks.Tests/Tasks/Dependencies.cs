using BaseTests;
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

	partial class Dependencies : BaseTest
	{
		[Test]
		public async Task GetFirstStartableTask_ReturnsNullWhenItsAlreadyStarted_()
		{
			await RunTest(GetFirstStartableTask_ReturnsNullWhenItsAlreadyStarted);
		}

		[Test]
		public async Task GetTopOfChain_ReturnsTopMostInCreatedState_()
		{
			await RunTest(GetTopOfChain_ReturnsTopMostInCreatedState);
		}

		[Test]
		public async Task MergingTwoChainsWorks_()
		{
			await RunTest(MergingTwoChainsWorks);
		}

		[Test]
		public async Task CurrentTaskWorks_()
		{
			await RunTest(CurrentTaskWorks);
		}
	}
}
