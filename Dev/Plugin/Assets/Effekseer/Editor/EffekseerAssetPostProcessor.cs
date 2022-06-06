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
			// Hack for EffekseerMaterial

			if (importedAssets.Any(_ => System.IO.Path.GetExtension(_) == ".asset"))
			{
				foreach (string assetPath in importedAssets)
				{
					if (Path.GetExtension(assetPath) == ".asset")
					{
						var asset = AssetDatabase.LoadAssetAtPath<EffekseerMaterialAsset>(assetPath);

						if (asset != null)
						{
							asset.AttachShader(assetPath);
						}
					}
				}
			}

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
				if (Path.GetExtension(assetPath) == ".efkcurve")
				{
					EffekseerCurveAsset.CreateAsset(assetPath);
				}
				if (Path.GetExtension(assetPath) == ".efkmat")
				{
					EffekseerMaterialAsset.ImportingAsset importingAsset = new EffekseerMaterialAsset.ImportingAsset();
					importingAsset.Data = System.IO.File.ReadAllBytes(assetPath);
					importingAsset.UserTextureSlotMax = EffekseerTool.Constant.UserTextureSlotCount;
					var info = new Effekseer.Editor.Utils.MaterialInformation();
					info.Load(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), assetPath));

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
						tp.Type = (EffekseerMaterialAsset.TextureType)EffekseerTool.Utl.TextureType.Color;
						importingAsset.Textures.Add(tp);
					}

					// TODO : Refactor
					foreach (var g in info.FixedGradients)
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

						importingAsset.FixedGradients.Add(gp);
					}

					foreach (var g in info.Gradients)
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
				if (Path.GetExtension(assetPath) == ".efkmatd")
				{
					EffekseerMaterialAsset.ImportingAsset importingAsset = new EffekseerMaterialAsset.ImportingAsset();
					importingAsset.Data = System.IO.File.ReadAllBytes(assetPath);
					importingAsset.IsCacheFile = true;

					EffekseerMaterialAsset.CreateAsset(assetPath, importingAsset);
				}
				if (Path.GetExtension(assetPath) == ".efkproj" || Path.GetExtension(assetPath) == ".efkefc")
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
}
#endif