// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace Unity.Editor.Tasks
{
	using Unity.Editor.Tasks.Internal.IO;

#if !UNITY_EDITOR

	public class SerializeFieldAttribute : Attribute
	{

	}

	public class ScriptableSingleton<T>
		where T : class, new()
	{
		private static T _instance;
		public static T instance => _instance ?? (_instance = new T());

		protected void Save(bool flush)
		{}
	}

	public static class Application
	{
		public static string productName { get; } = "DefaultApplication";
		public static string unityVersion { get; set; } = "2019.2.1f1";
		public static string projectPath { get; set; }
	}

	public static class EditorApplication
	{
		public static string applicationPath { get; set; }
		public static string applicationContentsPath { get; set; }
	}
#endif

	public sealed class TheEnvironment : ScriptableSingleton<TheEnvironment>
	{
		[NonSerialized] private IEnvironment environment;
		[SerializeField] private string unityApplication;
		[SerializeField] private string unityApplicationContents;
		[SerializeField] private string unityVersion;
		[SerializeField] private string projectPath;

		public void Flush()
		{
			unityApplication = Environment.UnityApplication;
			unityApplicationContents = Environment.UnityApplicationContents;
			unityVersion = Environment.UnityVersion;
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
					if (projectPath == null)
					{
#if UNITY_EDITOR
						projectPath = ".".ToSPath().Resolve().ToString(SlashMode.Forward);
#else
						projectPath = Application.projectPath;
#endif
						unityVersion = Application.unityVersion;
						unityApplication = EditorApplication.applicationPath;
						unityApplicationContents = EditorApplication.applicationContentsPath;
					}

					environment.Initialize(projectPath, unityVersion, unityApplication, unityApplicationContents);
					Flush();
				}
				return environment;
			}
		}
	}
}
