// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Editor.Tasks
{
	public sealed class TheEnvironment : ScriptableSingleton<TheEnvironment>
	{
		[NonSerialized] private IEnvironment environment;
		[SerializeField] private string unityApplication;
		[SerializeField] private string unityApplicationContents;
		[SerializeField] private string unityAssetsPath;
		[SerializeField] private string unityVersion;
		[SerializeField] private string projectPath;

		public void Flush()
		{
#if UNITY_EDITOR
			unityApplication = Environment.UnityApplication;
			unityApplicationContents = Environment.UnityApplicationContents;
			unityVersion = Environment.UnityVersion;
#endif
			unityAssetsPath = Environment.UnityAssetsPath;
			Save(true);
		}

		public static string ApplicationName { get; set; }

		public IEnvironment Environment
		{
			get
			{
				if (environment == null)
				{
					environment = new UnityEnvironment(ApplicationName ?? Application.productName);
					if (unityAssetsPath == null)
					{
						projectPath = System.IO.Path.GetFullPath(".");
						unityApplication = EditorApplication.applicationPath;
						unityApplicationContents = EditorApplication.applicationContentsPath;
						unityVersion = Application.unityVersion;
						unityAssetsPath = Application.dataPath;
					}

					environment.Initialize(projectPath, unityAssetsPath, unityVersion, unityApplication);
					Flush();
				}
				return environment;
			}
		}
	}
}
