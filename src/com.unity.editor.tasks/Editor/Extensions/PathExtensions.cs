// Copyright 2016-2019 Andreia Gaita
// Copyright 2015-2018 GitHub
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.IO;

namespace Unity.Editor.Tasks.Extensions
{
	public static class PathExtensions
	{
		public static string ToMD5(this string path)
		{
			byte[] computeHash;
			using (var hash = System.Security.Cryptography.MD5.Create())
			{
				using (var stream = File.OpenRead(path))
				{
					computeHash = hash.ComputeHash(stream);
				}
			}

			return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
		}

		public static string ToSha256(this string path)
		{
			byte[] computeHash;
			using (var hash = System.Security.Cryptography.SHA256.Create())
			{
				using (var stream = File.OpenRead(path))
				{
					computeHash = hash.ComputeHash(stream);
				}
			}

			return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
		}
	}

}
