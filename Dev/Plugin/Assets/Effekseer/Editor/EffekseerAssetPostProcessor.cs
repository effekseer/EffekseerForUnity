using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Effekseer.Editor
{
	public class EffekseerAssetPostProcessor : AssetPostprocessor
	{
		static EffekseerAssetPostProcessor()
		{
			EffekseerTool.Core.OnOutputLog += OutputLog;
		}

		static void OutputLog(EffekseerTool.LogLevel logLevel, string message)
		{
			if (logLevel == EffekseerTool.LogLevel.Info)
			{
				UnityEngine.Debug.Log(message);
			}
			else if (logLevel == EffekseerTool.LogLevel.Warning)
			{
				UnityEngine.Debug.LogWarning(message);
			}
		}

		static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromPaths)
		{
			foreach (string assetPath in importedAssets)
			{
				if (Path.GetExtension(assetPath) == ".efk")
				{
					EffekseerEffectAsset.CreateAsset(assetPath);
				}
				if (Path.GetExtension(assetPath) == ".efkmodel")
				{
					EffekseerModelAsset.CreateAsset(assetPath);
				}
				if (Path.GetExtension(assetPath) == ".efkmat")
				{
					EffekseerMaterialAsset.CreateAsset(assetPath, false);
				}
				if (Path.GetExtension(assetPath) == ".efkmatd")
				{
					EffekseerMaterialAsset.CreateAsset(assetPath, true);
				}
				if (Path.GetExtension(assetPath) == ".efkproj")
				{
					EffekseerTool.Core.LoadFrom(assetPath);
					var exporter = new EffekseerTool.Binary.Exporter();
					var data = exporter.Export(1);
					EffekseerEffectAsset.CreateAsset(assetPath, data);
				}
				if (Path.GetExtension(assetPath) == ".efkefc")
				{
					var fullpath = System.IO.Path.GetFullPath(assetPath);
					if (!System.IO.File.Exists(fullpath)) return;
					var allData = System.IO.File.ReadAllBytes(fullpath);

					if (allData.Length < 24) return;

					if (allData[0] != 'E' ||
						allData[1] != 'F' ||
						allData[2] != 'K' ||
						allData[3] != 'E')
					{
						return;
					}

					var version = System.BitConverter.ToInt32(allData, 4);

					var chunkData = allData.Skip(8).ToArray();

					var chunk = new EffekseerTool.IO.Chunk();
					chunk.Load(chunkData);

					var binBlock = chunk.Blocks.FirstOrDefault(_ => _.Chunk == "BIN_");
					if (binBlock == null)
					{
						return;
					}

					EffekseerEffectAsset.CreateAsset(assetPath, binBlock.Buffer);
				}
			}
		}
	}
}
#endif