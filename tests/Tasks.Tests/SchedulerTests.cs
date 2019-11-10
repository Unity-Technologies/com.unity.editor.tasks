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
		public void ChainingOnDifferentSchedulers_()
		{
			RunTest(ChainingOnDifferentSchedulers);
		}

		[Test]
		public void ConcurrentSchedulerDoesNotGuaranteeOrdering_()
		{
			RunTest(ConcurrentSchedulerDoesNotGuaranteeOrdering);
		}

		[Test]
		public void ConcurrentSchedulerWithDependencyOrdering_()
		{
			RunTest(ConcurrentSchedulerWithDependencyOrdering);
		}

		[Test]
		public void ExclusiveSchedulerGuaranteesOrdering_()
		{
			RunTest(ExclusiveSchedulerGuaranteesOrdering);
		}

		[Test]
		public void NonUITasksAlwaysRunOnDifferentThreadFromUITasks_()
		{
			RunTest(NonUITasksAlwaysRunOnDifferentThreadFromUITasks);
		}

		[Test]
		public void UISchedulerGuaranteesOrdering_()
		{
			RunTest(UISchedulerGuaranteesOrdering);
		}

	}
}
