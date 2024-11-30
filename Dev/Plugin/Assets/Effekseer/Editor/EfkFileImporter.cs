#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Effekseer.Editor
{
	public static class EfkFileImporter
	{
		private const string OpenEfkFileDirectoryCacheKey = "OpenEfkFileDirectoryCache";

		[MenuItem("Assets/Effekseer/Import Efk File", priority = 20)]
		public static void Import()
		{
			var sourceFilePath = SelectSourceFile();
			if (string.IsNullOrEmpty(sourceFilePath))
				return;

			// Save the directory of the files you selected this time,
			// because it is easier to select the same directory first the next time you import.
			PlayerPrefs.SetString(OpenEfkFileDirectoryCacheKey, Path.GetDirectoryName(sourceFilePath));
			PlayerPrefs.Save();

			var resourceFilePaths = LoadResourceFilePaths(sourceFilePath);
			var sourceDirectory = Path.GetDirectoryName(sourceFilePath);

			ValidateFilePaths(sourceFilePath, sourceDirectory, resourceFilePaths);

			var savePath = SelectSavePath(sourceFilePath);
			if (string.IsNullOrEmpty(savePath))
				return;
			// Convert local path to absolute path, because not use Unity's function.
			savePath = savePath.Replace("Assets", Application.dataPath);
			var saveDirectory = Path.GetDirectoryName(savePath);

			foreach (var path in resourceFilePaths)
			{
				var sourcePath = Path.Combine(sourceDirectory, path);
				var destPath = Path.Combine(saveDirectory, path);
				CopyFile(sourcePath, destPath);
			}
			CopyFile(sourceFilePath, savePath);
			AssetDatabase.Refresh();
		}

		/// <summary>
		///  Select the Efk file to be imported
		/// </summary>
		/// <returns></returns>
		private static string SelectSourceFile()
		{
			var defaultDir = PlayerPrefs.GetString(OpenEfkFileDirectoryCacheKey, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
			return EditorUtility.OpenFilePanelWithFilters("Select Efk File", defaultDir, new string[] { "Efk files", "efk" });
		}

		/// <summary>
		/// Select the save to path
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		private static string SelectSavePath(string filePath)
		{
			var fileName = Path.GetFileName(filePath);
			return EditorUtility.SaveFilePanelInProject("Select an import path", fileName, "efk", "Please enter a file path to import the efk to");
		}

		/// <summary>
		/// Load a list of resource files to be used in the Efk file.
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private static List<string> LoadResourceFilePaths(string filePath)
		{
			var data = File.ReadAllBytes(filePath);
			var resourcePath = new EffekseerResourcePath();
			if (!EffekseerEffectAsset.ReadResourcePath(data, ref resourcePath))
				DisplayErrorDialog($"Failed to load {filePath}.");

			// Remove duplicate files from the list, just to be safe.
			var paths = new List<string>();
			paths.AddRange(resourcePath.TexturePathList);
			paths.AddRange(resourcePath.ModelPathList);
			paths.AddRange(resourcePath.CurvePathList);
			paths.AddRange(resourcePath.SoundPathList);
			return paths.Distinct().ToList();
		}

		/// <summary>
		/// Verify that all files to be copied exist.
		/// </summary>
		/// <param name="sourceFilePath"></param>
		/// <param name="sourceDirectory"></param>
		/// <param name="resourcePaths"></param>
		/// <exception cref="FileNotFoundException"></exception>
		private static void ValidateFilePaths(string sourceFilePath, string sourceDirectory, List<string> resourcePaths)
		{
			var notFoundFiles = new List<string>();
			if (!File.Exists(sourceFilePath))
				notFoundFiles.Add(sourceFilePath);
			notFoundFiles.AddRange(resourcePaths.Where(path => !File.Exists(Path.Combine(sourceDirectory, path))));
			if (notFoundFiles.Count > 0)
				DisplayErrorDialog($"These files are not found.\n{string.Join("\n", notFoundFiles)}");
		}

		/// <summary>
		/// Copies the specified file to the specified path.
		/// </summary>
		/// <param name="sourceFilePath"></param>
		/// <param name="destFilePath"></param>
		private static void CopyFile(string sourceFilePath, string destFilePath)
		{
			try
			{
				var targetDirectory = new DirectoryInfo(Path.GetDirectoryName(destFilePath));
				if (!targetDirectory.Exists)
				{
					targetDirectory.Create();
					Debug.Log($"Create directory {targetDirectory.FullName}");
				}
				using (var destFile = File.Open(destFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
				{
					var data = File.ReadAllBytes(sourceFilePath);
					destFile.Write(data, 0, data.Length);
				}
				Debug.Log($"Copy complete. {sourceFilePath} to {destFilePath}");
			}
			catch (Exception e)
			{
				DisplayErrorDialog(e.Message);
			}
		}

		/// <summary>
		/// Show an error dialog.
		/// </summary>
		/// <param name="title"></param>
		/// <param name="message"></param>
		private static void DisplayErrorDialog(string message)
		{
			EditorUtility.DisplayDialog("Error!", message, "OK");
			throw new Exception(message);
		}
	}
}
#endif