namespace Unity.Editor.Tasks
{
	public class FirstResultOutputProcessor<T> : BaseOutputProcessor<T>
	{
		private readonly FuncO<string, T, bool> handler;
		private bool isSet = false;

		public FirstResultOutputProcessor(FuncO<string, T, bool> handler)
			: base()
		{
			this.handler = handler;
		}

		public override void LineReceived(string line)
		{
			if (!isSet)
			{
				if (handler(line, out T res))
				{
					Result = res;
					isSet = true;
					RaiseOnEntry(res);
				}
			}
		}
	}
}
