// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Unity.Editor.Tasks
{
	using Internal.IO;

	public class ProcessManager : IProcessManager
	{
		private readonly HashSet<IProcess> processes = new HashSet<IProcess>();

		public ProcessManager(IEnvironment environment)
		{
			DefaultProcessEnvironment = new ProcessEnvironment(environment);
		}

		public T Configure<T>(T processTask, string workingDirectory = null)
				where T : IProcessTask
		{
			var startInfo = new ProcessStartInfo {
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8
			};

			startInfo.Configure(processTask.ProcessEnvironment, workingDirectory);

			startInfo.FileName = processTask.ProcessName.ToSPath().ToString();
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

		public IProcessEnvironment DefaultProcessEnvironment { get; }
	}
}
