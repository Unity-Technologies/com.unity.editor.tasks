using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseTests;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace OutputProcessorTests
{
	partial class FirstResultOutputProcessorTests : BaseTest
	{
		[Test]
		public void OnlyRaisesOnce()
		{
			int called = 0;
			var processor = new FirstResultOutputProcessor<int>(s => int.Parse(s));
			processor.OnEntry += _ => called++;
			processor.Process("nothing");
			processor.Process("100");
			processor.Process("200");

			Assert.AreEqual(1, called);
			Assert.AreEqual(100, processor.Result);
		}
	}
}
