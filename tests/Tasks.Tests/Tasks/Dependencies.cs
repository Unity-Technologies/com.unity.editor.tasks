using BaseTests;
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
	}
}
