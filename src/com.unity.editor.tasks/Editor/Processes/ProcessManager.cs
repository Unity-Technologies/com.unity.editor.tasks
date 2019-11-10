// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Unity.Editor.Tasks
{
	using Logging;

	public class ProcessManager : IProcessManager
	{
		private static readonly ILogging logger = LogHelper.GetLogger<ProcessManager>();

		private readonly IEnvironment environment;
		private readonly HashSet<IProcess> processes = new HashSet<IProcess>();

		public ProcessManager(IEnvironment environment,
			CancellationToken cancellationToken)
		{
			Instance = this;
			this.environment = environment;
			DefaultProcessEnvironment = new ProcessEnvironment(environment);
			CancellationToken = cancellationToken;
		}

		public T Configure<T>(T processTask,
			string workingDirectory = null,
			bool withInput = false)
				where T : IProcessTask
		{
			var startInfo = new ProcessStartInfo {
				RedirectStandardInput = withInput,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8
			};

			startInfo.Configure(processTask.ProcessEnvironment, workingDirectory);

			startInfo.FileName = processTask.ProcessName;
			startInfo.Arguments = processTask.ProcessArguments;
			processTask.Configure(startInfo);
			processTask.OnStartProcess += p => processes.Add(p);
			processTask.OnEndProcess += p => {
				if (processes.Contains(p))
					processes.Remove(p);
			};
			return processTask;
		}

		public void Stop()
		{
			foreach (var p in processes.ToArray())
				p.Stop();
		}

		public static IProcessManager Instance { get; private set; }

		public CancellationToken CancellationToken { get; }

		public IProcessEnvironment DefaultProcessEnvironment { get; }
	}
}
