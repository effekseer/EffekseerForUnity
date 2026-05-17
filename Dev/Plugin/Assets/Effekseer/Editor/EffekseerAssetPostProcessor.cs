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
			AttachShadersToImportedMaterialAssets(importedAssets);

			// Effects resolve generated Effekseer assets by path, so create those assets first.
			foreach (string assetPath in importedAssets)
			{
				ImportResourceAsset(assetPath);
			}

			foreach (string assetPath in importedAssets)
			{
				ImportEffectAsset(assetPath);
			}
		}

		static void AttachShadersToImportedMaterialAssets(string[] importedAssets)
		{
			// Hack for EffekseerMaterial
			if (!importedAssets.Any(_ => IsExtension(_, ".asset")))
			{
				return;
			}

			foreach (string assetPath in importedAssets)
			{
				if (!IsExtension(assetPath, ".asset"))
				{
					continue;
				}

				var asset = AssetDatabase.LoadAssetAtPath<EffekseerMaterialAsset>(assetPath);

				if (asset != null)
				{
					asset.AttachShader(assetPath);
				}
			}
		}

		static void ImportResourceAsset(string assetPath)
		{
			if (IsExtension(assetPath, ".efkmodel"))
			{
				EffekseerModelAsset.CreateAsset(assetPath);
			}
			else if (IsExtension(assetPath, ".efkcurve"))
			{
				EffekseerCurveAsset.CreateAsset(assetPath);
			}
			else if (IsExtension(assetPath, ".efkmat"))
			{
				ImportMaterialAsset(assetPath);
			}
			else if (IsExtension(assetPath, ".efkmatd"))
			{
				EffekseerMaterialAsset.ImportingAsset importingAsset = new EffekseerMaterialAsset.ImportingAsset();
				importingAsset.Data = System.IO.File.ReadAllBytes(assetPath);
				importingAsset.IsCacheFile = true;

				EffekseerMaterialAsset.CreateAsset(assetPath, importingAsset);
			}
		}

		static void ImportEffectAsset(string assetPath)
		{
			if (IsExtension(assetPath, ".efk"))
			{
				EffekseerEffectAsset.CreateAsset(assetPath);
			}
			else if (IsExtension(assetPath, ".efkproj") || IsExtension(assetPath, ".efkefc"))
			{
				ImportProjectAsset(assetPath);
			}
		}

		static void ImportMaterialAsset(string assetPath)
		{
			EffekseerMaterialAsset.ImportingAsset importingAsset = new EffekseerMaterialAsset.ImportingAsset();
			importingAsset.UserTextureSlotMax = EffekseerTool.Constant.UserTextureSlotCount;
			var info = new Effekseer.Editor.Utils.MaterialInformation();
			var materialPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), assetPath);
			if (!info.Load(materialPath))
			{
				UnityEngine.Debug.LogWarning(CreateMaterialImportWarning(assetPath, info));
				return;
			}

			importingAsset.Data = System.IO.File.ReadAllBytes(materialPath);
			importingAsset.CustomData1Count = info.CustomData1Count;
			importingAsset.CustomData2Count = info.CustomData2Count;
			importingAsset.HasRefraction = info.HasRefraction;
			importingAsset.ShadingModel = info.ShadingModel;

			foreach (var u in info.Uniforms)
			{
				var up = new EffekseerMaterialAsset.UniformProperty();
				up.Name = u.Name;
				up.UniformName = u.UniformName;
				up.Count = u.Type;
				importingAsset.Uniforms.Add(up);
			}

			foreach (var t in info.Textures)
			{
				var tp = new EffekseerMaterialAsset.TextureProperty();
				tp.Name = t.Name;
				tp.UniformName = t.UniformName;
				tp.Type = (EffekseerMaterialAsset.TextureType)t.Type;
				tp.Index = t.Index;
				importingAsset.Textures.Add(tp);
			}

			// TODO : Refactor
			foreach (var g in info.FixedGradients)
			{
				var gp = CreateGradientProperty(g);
				importingAsset.FixedGradients.Add(gp);
			}

			foreach (var g in info.Gradients)
			{
				var gp = CreateGradientProperty(g);
				importingAsset.Gradients.Add(gp);
			}

			importingAsset.IsCacheFile = false;
			importingAsset.Code = info.Code;

			importingAsset.MaterialRequiredFunctionTypes = new EffekseerMaterialAsset.MaterialRequiredFunctionType[info.RequiredFunctionTypes.Length];
			for (int i = 0; i < importingAsset.MaterialRequiredFunctionTypes.Length; i++)
			{
				importingAsset.MaterialRequiredFunctionTypes[i] = (EffekseerMaterialAsset.MaterialRequiredFunctionType)info.RequiredFunctionTypes[i];
			}

			EffekseerMaterialAsset.CreateAsset(assetPath, importingAsset);
		}

		static string CreateMaterialImportWarning(string assetPath, Effekseer.Editor.Utils.MaterialInformation info)
		{
			string reason = "Unknown error.";
			switch (info.LastErrorCode)
			{
				case Effekseer.Editor.Utils.MaterialInformationErrorCode.TooNewFormat:
					reason = "The material format is newer than this importer supports.";
					break;
				case Effekseer.Editor.Utils.MaterialInformationErrorCode.NotFound:
					reason = "The material file was not found.";
					break;
				case Effekseer.Editor.Utils.MaterialInformationErrorCode.FailedToOpen:
					reason = "The material file could not be opened.";
					break;
				case Effekseer.Editor.Utils.MaterialInformationErrorCode.InvalidFormat:
					reason = "The material file is invalid, truncated, or corrupted.";
					break;
			}

			string detail = string.IsNullOrEmpty(info.LastErrorMessage) ? "No additional details." : info.LastErrorMessage;
			string fileVersion = info.FileVersion == 0 ? "unknown" : info.FileVersion.ToString();

			return string.Format(
				"Failed to load Effekseer material: {0}. Reason: {1} Detail: {2} File version: {3}. Latest supported version: {4}.",
				assetPath,
				reason,
				detail,
				fileVersion,
				info.LatestSupportedVersion);
		}

		static EffekseerMaterialAsset.GradientProperty CreateGradientProperty(Utils.MaterialInformation.GradientInformation g)
		{
			var gp = new EffekseerMaterialAsset.GradientProperty();
			gp.Name = g.Name;
			gp.UniformName = g.UniformName;

			gp.ColorMarkers = new EffekseerMaterialAsset.GradientProperty.ColorMarker[g.Data.ColorMarkers.Length];
			for (int i = 0; i < g.Data.ColorMarkers.Length; i++)
			{
				gp.ColorMarkers[i].ColorR = g.Data.ColorMarkers[i].ColorR;
				gp.ColorMarkers[i].ColorG = g.Data.ColorMarkers[i].ColorG;
				gp.ColorMarkers[i].ColorB = g.Data.ColorMarkers[i].ColorB;
				gp.ColorMarkers[i].Intensity = g.Data.ColorMarkers[i].Intensity;
				gp.ColorMarkers[i].Position = g.Data.ColorMarkers[i].Position;
			}

			gp.AlphaMarkers = new EffekseerMaterialAsset.GradientProperty.AlphaMarker[g.Data.AlphaMarkers.Length];
			for (int i = 0; i < g.Data.AlphaMarkers.Length; i++)
			{
				gp.AlphaMarkers[i].Alpha = g.Data.AlphaMarkers[i].Alpha;
				gp.AlphaMarkers[i].Position = g.Data.AlphaMarkers[i].Position;
			}

			return gp;
		}

		static void ImportProjectAsset(string assetPath)
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
				// Before 1.5
				if (EffekseerTool.Core.LoadFrom(assetPath))
				{
					var exporter = new EffekseerTool.Binary.Exporter();
					var data = exporter.Export(1);
					EffekseerEffectAsset.CreateAsset(assetPath, data);
				}
			}
			else
			{
				// After 1.5
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

		static bool IsExtension(string path, string extension)
		{
			return string.Equals(Path.GetExtension(path), extension, System.StringComparison.OrdinalIgnoreCase);
		}
	}
}
#endif
