// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Diagnostics;

namespace Unity.Editor.Tasks
{
	using Logging;
	using Unity.Editor.Tasks.Helpers;

	public interface IProcessEnvironment
	{
		void Configure(ProcessStartInfo psi, string workingDirectory = null);
		IEnvironment Environment { get; }
	}

	public class ProcessEnvironment : IProcessEnvironment
	{
		public ProcessEnvironment(IEnvironment environment)
		{
			Logger = LogHelper.GetLogger(GetType());
			Environment = environment;
		}

		public virtual void Configure(ProcessStartInfo psi, string workingDirectory = null)
		{
			Guard.ArgumentNotNull(psi, "psi");
			workingDirectory = workingDirectory ?? Environment.UnityProjectPath;

			psi.WorkingDirectory = workingDirectory;

			var path = Environment.Path;
			psi.EnvironmentVariables["PROCESS_WORKINGDIR"] = workingDirectory;

			var pathEnvVarKey = Environment.GetEnvironmentVariableKey("PATH");
			psi.EnvironmentVariables["PROCESS_FULLPATH"] = path;
			psi.EnvironmentVariables[pathEnvVarKey] = path;
		}

		public IEnvironment Environment { get; private set; }
		protected ILogging Logger { get; private set; }
	}
}
