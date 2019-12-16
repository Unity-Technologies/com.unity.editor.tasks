namespace Unity.Editor.Tasks
{
	using System;

	/// <summary>
	/// Processor that returns one output of type <typeparamref name="T"/>
	/// from one or more string inputs. <see cref="BaseOutputProcessor{T}.RaiseOnEntry(T)"/>
	/// will only be called once on this processor.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FirstResultOutputProcessor<T> : BaseOutputProcessor<T>
	{
		private bool isSet = false;

		/// <summary>
		/// This constructor sets the processor to call the virtual <see cref="ProcessLine(string, out T)"/>
		/// method. Override it to process inputs.
		/// </summary>
		public FirstResultOutputProcessor(Func<string, T> converter)
			: base(converter)
		{}

		/// <summary>
		/// This constructor sets the <paramref name="handler"/> to be called
		/// for every input. The first time the handler returns true, its output
		/// will be set as the result of the processor.
		/// </summary>
		/// <param name="handler"></param>
		public FirstResultOutputProcessor(FuncO<string, T, bool> handler = null)
			: base(handler)
		{}

		protected override bool ProcessLine(string line, out T result)
		{
			result = default;
			if (isSet) return false;

			if (!base.ProcessLine(line, out result))
				return false;

			isSet = true;
			return true;
		}
	}
}
