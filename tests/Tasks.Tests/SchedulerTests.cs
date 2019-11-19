using System.Threading.Tasks;
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
	partial class SchedulerTests
	{
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
	}
}
