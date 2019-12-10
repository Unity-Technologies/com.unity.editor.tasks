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

	partial class SchedulerTests
	{
		[Test]
		public async Task CustomScheduler_Works_()
		{
			await RunTest(CustomScheduler_Works);
		}

		[Test]
		public async Task CustomScheduler_ChainRunsOnTheSameScheduler_()
		{
			await RunTest(CustomScheduler_ChainRunsOnTheSameScheduler);
		}

		[Test]
		public async Task CustomScheduler_AsyncKeepsOrder_()
		{
			await RunTest(CustomScheduler_AsyncKeepsOrder);
		}

		[Test]
		public async Task ChainingOnDifferentSchedulers_()
		{
			await RunTest(ChainingOnDifferentSchedulers);
		}

		[Test]
		public async Task ConcurrentSchedulerDoesNotGuaranteeOrdering_()
		{
			await RunTest(ConcurrentSchedulerDoesNotGuaranteeOrdering);
		}

		[Test]
		public async Task ConcurrentSchedulerWithDependencyOrdering_()
		{
			await RunTest(ConcurrentSchedulerWithDependencyOrdering);
		}

		[Test]
		public async Task ExclusiveSchedulerGuaranteesOrdering_()
		{
			await RunTest(ExclusiveSchedulerGuaranteesOrdering);
		}

		[Test]
		public async Task NonUITasksAlwaysRunOnDifferentThreadFromUITasks_()
		{
			await RunTest(NonUITasksAlwaysRunOnDifferentThreadFromUITasks);
		}

		[Test]
		public async Task UISchedulerGuaranteesOrdering_()
		{
			await RunTest(UISchedulerGuaranteesOrdering);
		}

		[Test]
		public async Task AsyncAwaitTasks_ExclusiveScheduler_RunInSchedulingOrder_()
		{
			await RunTest(AsyncAwaitTasks_ExclusiveScheduler_RunInSchedulingOrder);
		}
	}
}
