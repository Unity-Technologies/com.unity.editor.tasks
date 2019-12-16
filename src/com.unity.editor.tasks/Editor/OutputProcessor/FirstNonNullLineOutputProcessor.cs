namespace Unity.Editor.Tasks
{
	using System;

	public class FirstNonNullLineOutputProcessor<T> : FirstResultOutputProcessor<T>
	{
		public FirstNonNullLineOutputProcessor(Func<string, T> converter)
			: base(converter)
		{}

		public FirstNonNullLineOutputProcessor(FuncO<string, T, bool> handler = null)
			: base(handler)
		{}

		protected override bool ProcessLine(string line, out T result)
		{
			result = default;

			if (string.IsNullOrEmpty(line))
				return false;

			line = line.Trim();

			return base.ProcessLine(line, out result);
		}
	}
}
