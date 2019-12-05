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

	partial class TaskQueueTests
	{
		[Test]
		public async Task DoubleSchedulingStartsOnlyOnce_()
		{
			await RunTest(DoubleSchedulingStartsOnlyOnce);
		}
	}
}
