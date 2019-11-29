// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Diagnostics;

namespace Unity.Editor.Tasks
{
	using Internal.IO;
	using Helpers;

	public interface IProcessEnvironment
	{
		void Configure(ProcessStartInfo psi, string workingDirectory = null);
		IEnvironment Environment { get; }
	}

	class ProcessEnvironment : IProcessEnvironment
	{
		public ProcessEnvironment(IEnvironment environment)
		{
			Environment = environment;
		}

		public void Configure(ProcessStartInfo psi, string workingDirectory = null)
		{
			Guard.EnsureNotNull(psi, nameof(psi));
			workingDirectory = workingDirectory ?? Environment.UnityProjectPath;

			string normalizedWorkingDirectory = workingDirectory.ToSPath().ToString();

			psi.WorkingDirectory = normalizedWorkingDirectory;

			var path = Environment.Path;
			psi.EnvironmentVariables["PROCESS_WORKINGDIR"] = normalizedWorkingDirectory;

			var pathEnvVarKey = Environment.GetEnvironmentVariableKey("PATH");
			psi.EnvironmentVariables["PROCESS_FULLPATH"] = path;
			psi.EnvironmentVariables[pathEnvVarKey] = path;
		}

		public IEnvironment Environment { get; private set; }
	}
}
