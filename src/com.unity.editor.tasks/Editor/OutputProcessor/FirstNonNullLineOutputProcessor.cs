namespace Unity.Editor.Tasks
{
	using System;

	public class FirstNonNullLineOutputProcessor<T> : FirstResultOutputProcessor<T>
	{
		public FirstNonNullLineOutputProcessor(Func<string, T> converter = null)
			: base((string line, out T ret) => Parse(line, out ret, converter))
		{ }

		private static bool Parse(string line, out T result, Func<string, T> converter = null)
		{
			result = default;
			if (String.IsNullOrEmpty(line))
				return false;

			line = line.Trim();

			if (converter != null)
			{
				result = converter(line);
				return true;
			}

			result = (T)(object)line;
			return true;
		}
	}
}
