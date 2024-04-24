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

			// 次回インポートする際に同じディレクトリを最初に選択してくれたほうが楽なので、今回選択したファイルのディレクトリを保存しておく
			PlayerPrefs.SetString(OpenEfkFileDirectoryCacheKey, Path.GetDirectoryName(sourceFilePath));
			PlayerPrefs.Save();

			var resourceFilePaths = LoadResourceFilePaths(sourceFilePath);
			var sourceDirectory = Path.GetDirectoryName(sourceFilePath);

			ValidateFilePaths(sourceFilePath, sourceDirectory, resourceFilePaths);

			var savePath = SelectSavePath(sourceFilePath);
			if (string.IsNullOrEmpty(savePath))
				return;
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
		/// インポートするEfkファイルを選択する
		/// </summary>
		/// <returns></returns>
		private static string SelectSourceFile()
		{
			var defaultDir = PlayerPrefs.GetString(OpenEfkFileDirectoryCacheKey, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
			return EditorUtility.OpenFilePanelWithFilters("Select Efk File", defaultDir, new string[] { "Efk files", "efk" });
		}

		/// <summary>
		/// 保存先のパスを選択
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		private static string SelectSavePath(string filePath)
		{
			var fileName = Path.GetFileName(filePath);
			return EditorUtility.SaveFilePanelInProject("Select an import path", fileName, "efk", "Please enter a file path to import the efk to");
		}

		/// <summary>
		/// Efkファイルで使用するリソースファイルの一覧をロードする
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private static List<string> LoadResourceFilePaths(string filePath)
		{
			var data = File.ReadAllBytes(filePath);
			var resourcePath = new EffekseerResourcePath();
			if (!EffekseerEffectAsset.ReadResourcePath(data, ref resourcePath))
				DisplayErrorDialog("Error", $"Failed to load {filePath}.");

			// 念の為、重複したファイルを排除したリストを返却する
			var paths = new List<string>();
			paths.AddRange(resourcePath.TexturePathList);
			paths.AddRange(resourcePath.ModelPathList);
			paths.AddRange(resourcePath.CurvePathList);
			paths.AddRange(resourcePath.SoundPathList);
			return paths.Distinct().ToList();
		}

		/// <summary>
		/// コピー対象のファイルがすべて存在しているか確認する
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
				DisplayErrorDialog("エラー", $"These files are not found.\n{string.Join("\n", notFoundFiles)}");
		}

		/// <summary>
		/// ファイルをコピーする
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
				DisplayErrorDialog("Error", e.Message);
			}
		}

		/// <summary>
		/// エラーダイアログを表示する
		/// </summary>
		/// <param name="title"></param>
		/// <param name="message"></param>
		private static void DisplayErrorDialog(string title, string message)
		{
			EditorUtility.DisplayDialog(title, message, "OK");
			throw new Exception(message);
		}
	}
}
