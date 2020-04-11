//#define EFFEKSEER_INTERNAL_DEBUG

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Effekseer.Internal
{
	enum AlphaBlendType : int
	{
		Opacity = 0,
		Blend = 1,
		Add = 2,
		Sub = 3,
		Mul = 4,
	}

	struct MaterialKey
	{
		public bool ZTest;
		public bool ZWrite;
		public AlphaBlendType Blend;
		public int Cull;

		public int GetKey()
		{
			return (int)Blend +
				(ZTest ? 1 : 0) << 4 +
				(ZWrite ? 1 : 0) << 5 +
				Cull << 6;
		}
	}
	class MaterialCollection
	{
		public Shader Shader;
		public string[] Keywords = new string[0];

		Dictionary<int, Material> materials = new Dictionary<int, Material>();

		public Material GetMaterial(ref MaterialKey key)
		{
			var id = key.GetKey();

			if (materials.ContainsKey(id)) return materials[id];

			var material = new Material(Shader);

			if (key.Blend == AlphaBlendType.Opacity)
			{
				material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.One);
				material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.Zero);
				material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
			}
			else if (key.Blend == AlphaBlendType.Blend)
			{
				material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
			}
			else if (key.Blend == AlphaBlendType.Add)
			{
				material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.One);
				material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
			}
			else if (key.Blend == AlphaBlendType.Mul)
			{
				material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.Zero);
				material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.SrcColor);
				material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
			}
			else if (key.Blend == AlphaBlendType.Sub)
			{
				material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.One);
				material.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.ReverseSubtract);
			}

			material.SetFloat("_ZTest", key.ZTest ? (float)UnityEngine.Rendering.CompareFunction.LessEqual : (float)UnityEngine.Rendering.CompareFunction.Disabled);
			material.SetFloat("_ZWrite", key.ZWrite ? 1.0f : 0.0f);
			material.SetFloat("_Cull", key.Cull);

			foreach(var keyword in Keywords)
			{
				material.EnableKeyword(keyword);
			}

			materials.Add(id, material);

			return material;
		}
	}

	/*
	struct UnityRendererMaterialUniformParameter
	{
		public string Name;
		public int Offset;
		public int Count;
	}

	struct UnityRendererMaterialTextureParameter
	{
		public string Name;
	}
	*/

	class UnityRendererMaterial
	{
		internal EffekseerMaterialAsset asset;
		internal MaterialCollection materials = new MaterialCollection();
		internal MaterialCollection materialsModel = new MaterialCollection();
		internal MaterialCollection materialsRefraction = null;
		internal MaterialCollection materialsModelRefraction = null;

		public UnityRendererMaterial(EffekseerMaterialAsset asset)
		{
			this.asset = asset;
			materials.Shader = asset.shader;
			materialsModel.Shader = asset.shader;
			materialsModel.Keywords = new string[] { "_MODEL_" };

			if(asset.HasRefraction)
			{
				materialsRefraction = new MaterialCollection();
				materialsModelRefraction = new MaterialCollection();
				materialsRefraction.Shader = asset.shader;
				materialsRefraction.Keywords = new string[] { "_MATERIAL_REFRACTION_" };
				materialsModelRefraction.Shader = asset.shader;
				materialsModelRefraction.Keywords = new string[] { "_MODEL_", "_MATERIAL_REFRACTION_" };


			}
		}
	}

	class UnityRendererModel : IDisposable
	{
		public ComputeBuffer VertexBuffer;
		public ComputeBuffer IndexBuffer;
		public ComputeBuffer VertexOffsets;
		public ComputeBuffer IndexOffsets;

		public List<int> IndexCounts = new List<int>();

		public List<int> vertexOffsets = new List<int>();
		public List<int> indexOffsets = new List<int>();


		public unsafe void Initialize(byte[] buffer)
		{
			int sizeEffekseerVertex = 4 * 15;

			int version = 0;
			int offset = 0;
			version = BitConverter.ToInt32(buffer, offset);
			offset += sizeof(int);

			if(version < 1)
			{
				sizeEffekseerVertex -= 4;
			}

			if (version == 2 || version >= 5)
			{
				// float scale = (unused)
				BitConverter.ToSingle(buffer, offset);
				offset += sizeof(float);
			}

			// int modelCount = (unused)
			BitConverter.ToInt32(buffer, offset);
			offset += sizeof(int);

			int frameCount = 0;

			if (version >= 5)
			{
				frameCount = BitConverter.ToInt32(buffer, offset);
				offset += sizeof(int);
			}
			else
			{
				frameCount = 1;
			}

			var offsetBack = offset;

			int vertexBufferCount = 0;
			int indexBufferCount = 0;

			for (int fi = 0; fi < frameCount; fi++)
			{
				vertexOffsets.Add(vertexBufferCount);
				int vertexCount = BitConverter.ToInt32(buffer, offset);
				offset += sizeof(int);

				vertexBufferCount += vertexCount;
				offset += sizeEffekseerVertex * vertexCount;

				indexOffsets.Add(indexBufferCount);

				int faceCount = BitConverter.ToInt32(buffer, offset);

				offset += sizeof(int);

				indexBufferCount += 3 * faceCount;
				offset += sizeof(int) * (3 * faceCount);

				IndexCounts.Add(3 * faceCount);
			}

			VertexBuffer = new ComputeBuffer(vertexBufferCount, sizeof(Vertex));
			IndexBuffer = new ComputeBuffer(indexBufferCount, sizeof(int));
			offset = offsetBack;

			List<Vertex> vertex = new List<Vertex>();
			List<int> index = new List<int>();

			for (int fi = 0; fi < frameCount; fi++)
			{
				int vertexCount = BitConverter.ToInt32(buffer, offset);
				offset += sizeof(int);

				if(version < 1)
				{
					fixed (byte* vs_ = &buffer[offset])
					{
						InternalVertexV0* vs = (InternalVertexV0*)vs_;

						for (int vi = 0; vi < vertexCount; vi++)
						{
							Vertex v;
							v.Position = vs[vi].Position;
							v.UV = vs[vi].UV;
							v.Normal = vs[vi].Normal;
							v.Tangent = vs[vi].Tangent;
							v.Binormal = vs[vi].Binormal;
							v.VColor.r = 1.0f;
							v.VColor.g = 1.0f;
							v.VColor.b = 1.0f;
							v.VColor.a = 1.0f;
							vertex.Add(v);
						}
					}
				}
				else
				{
					fixed (byte* vs_ = &buffer[offset])
					{
						InternalVertex* vs = (InternalVertex*)vs_;

						for (int vi = 0; vi < vertexCount; vi++)
						{
							Vertex v;
							v.Position = vs[vi].Position;
							v.UV = vs[vi].UV;
							v.Normal = vs[vi].Normal;
							v.Tangent = vs[vi].Tangent;
							v.Binormal = vs[vi].Binormal;
							v.VColor.r = vs[vi].VColor.r / 255.0f;
							v.VColor.g = vs[vi].VColor.g / 255.0f;
							v.VColor.b = vs[vi].VColor.b / 255.0f;
							v.VColor.a = vs[vi].VColor.a / 255.0f;
							vertex.Add(v);
						}
					}
				}

				offset += sizeEffekseerVertex * vertexCount;

				int faceCount = BitConverter.ToInt32(buffer, offset);
				offset += sizeof(int);

				for (int ffi = 0; ffi < faceCount; ffi++)
				{
					int f1 = BitConverter.ToInt32(buffer, offset);
					offset += sizeof(int);

					int f2 = BitConverter.ToInt32(buffer, offset);
					offset += sizeof(int);

					int f3 = BitConverter.ToInt32(buffer, offset);
					offset += sizeof(int);

					index.Add(f1);
					index.Add(f2);
					index.Add(f3);

				}

				
			}

			VertexBuffer.SetData(vertex, 0, 0, vertex.Count);
			IndexBuffer.SetData(index, 0, 0, index.Count);

			VertexOffsets = new ComputeBuffer(vertexOffsets.Count, sizeof(int));
			IndexOffsets = new ComputeBuffer(indexOffsets.Count, sizeof(int));
			VertexOffsets.SetData(vertexOffsets);
			IndexOffsets.SetData(indexOffsets);
		}

		public void Dispose()
		{
			if (VertexBuffer != null)
			{
				VertexBuffer.Dispose();
				VertexBuffer = null;
			}

			if (IndexBuffer != null)
			{
				IndexBuffer.Dispose();
				IndexBuffer = null;
			}

			if (VertexOffsets != null)
			{
				VertexOffsets.Dispose();
				VertexOffsets = null;
			}

			if (IndexOffsets != null)
			{
				IndexOffsets.Dispose();
				IndexOffsets = null;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	struct InternalVertex
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector3 Binormal;
		public Vector3 Tangent;
		public Vector2 UV;
		public Color32 VColor;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct InternalVertexV0
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector3 Binormal;
		public Vector3 Tangent;
		public Vector2 UV;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct Vertex
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector3 Binormal;
		public Vector3 Tangent;
		public Vector2 UV;
		public Color VColor;
	}

	internal class EffekseerRendererUnity : IEffekseerRenderer
	{
		const CameraEvent cameraEvent = CameraEvent.AfterForwardAlpha;
		const int VertexSize = 36;
		const int VertexDistortionSize = 36 + 4 * 6;
		const int VertexDynamicSize = 4 * 17;

		class MaterialPropCollection
		{
			List<MaterialPropertyBlock> materialPropBlocks = new List<MaterialPropertyBlock>();
			int materialPropBlockOffset = 0;

			public void Reset()
			{
				materialPropBlockOffset = 0;
			}

			public MaterialPropertyBlock GetNext()
			{
				if (materialPropBlockOffset >= materialPropBlocks.Count)
				{
					materialPropBlocks.Add(new MaterialPropertyBlock());
				}

				var ret = materialPropBlocks[materialPropBlockOffset];
				materialPropBlockOffset++;
				return ret;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct CustomDataBuffer
		{
			public float V1;
			public float V2;
			public float V3;
			public float V4;
		}

		class CustomDataBufferCollection
		{
			const int elementCount = 40;

			List<ComputeBuffer> computeBuffers = new List<ComputeBuffer>();
			List<CustomDataBuffer[]> cpuBuffers = new List<CustomDataBuffer[]>();
			int bufferOffset = 0;

			public CustomDataBufferCollection()
			{
				for (int i = 0; i < 10; i++)
				{
					computeBuffers.Add(new ComputeBuffer(elementCount, sizeof(float) * 4));
					cpuBuffers.Add(new CustomDataBuffer[elementCount]);
				}
			}

			public void Reset()
			{
				bufferOffset = 0;
			}

			public unsafe int Allocate(CustomDataBuffer* param, int offset, int count, ref ComputeBuffer computeBuffer)
			{
				if (bufferOffset >= computeBuffers.Count)
				{
					computeBuffers.Add(new ComputeBuffer(elementCount, sizeof(float) * 4));
					cpuBuffers.Add(new CustomDataBuffer[elementCount]);
				}

				computeBuffer = computeBuffers[bufferOffset];
				var cpuBuffer = cpuBuffers[bufferOffset];
				bufferOffset++;

				if (count >= elementCount)
				{
					count = elementCount;
				}

				for (int i = 0; i < count; i++)
				{
					cpuBuffer[i] = param[offset + i];
				}

				computeBuffer.SetData(cpuBuffer, 0, 0, count);

				return count;
			}

			public void Release()
			{
				for (int i = 0; i < computeBuffers.Count; i++)
				{
					computeBuffers[i].Release();
				}
				computeBuffers.Clear();
				cpuBuffers.Clear();
			}
		}


		class ModelBufferCollection
		{
			const int elementCount = 40;

			List<ComputeBuffer> computeBuffers = new List<ComputeBuffer>();
			List<Plugin.UnityRenderModelParameter[]> cpuBuffers = new List<Plugin.UnityRenderModelParameter[]>();
			int bufferOffset = 0;

			public ModelBufferCollection()
			{
				for (int i = 0; i < 10; i++)
				{
					computeBuffers.Add(new ComputeBuffer(elementCount, sizeof(int) * 25));
					cpuBuffers.Add(new Plugin.UnityRenderModelParameter[elementCount]);
				}
			}

			public void Reset()
			{
				bufferOffset = 0;
			}

			public unsafe int Allocate(Plugin.UnityRenderModelParameter* param, int offset, int count, ref ComputeBuffer computeBuffer)
			{
				if (bufferOffset >= computeBuffers.Count)
				{
					computeBuffers.Add(new ComputeBuffer(elementCount, sizeof(int) * 25));
					cpuBuffers.Add(new Plugin.UnityRenderModelParameter[elementCount]);
				}

				computeBuffer = computeBuffers[bufferOffset];
				var cpuBuffer = cpuBuffers[bufferOffset];
				bufferOffset++;

				if (count >= elementCount)
				{
					count = elementCount;
				}

				for (int i = 0; i < count; i++)
				{
					cpuBuffer[i] = param[offset + i];
				}

				computeBuffer.SetData(cpuBuffer, 0, 0, count);

				return count;
			}

			public void Release()
			{
				for (int i = 0; i < computeBuffers.Count; i++)
				{
					computeBuffers[i].Release();
				}
				computeBuffers.Clear();
				cpuBuffers.Clear();
			}
		}

		class DelayEvent
		{
			public int RestTime = 0;
			public virtual void Call() { }
		}

		class DelayEventDisposeComputeBuffer : DelayEvent
		{
			ComputeBuffer cb = null;

			public DelayEventDisposeComputeBuffer(ComputeBuffer cb)
			{
				this.cb = cb;
			}

			public override void Call()
			{
				if(cb != null)
				{
					cb.Dispose();
					cb = null;
				}
			}
		}


		private class ComputeBufferCollection : IDisposable
		{
			/// <summary>
			/// SimpleSprite * 4 * 8192
			/// </summary>
			int VertexMaxSize = 8192 * 4 * 36;

			const int defaultVertexSize = 36;

			Dictionary<int, ComputeBuffer> computeBuffers = new Dictionary<int, ComputeBuffer>();
			byte[] data = null;

			public ComputeBufferCollection()
			{
				data = new byte[VertexMaxSize];
				Get(defaultVertexSize);
			}

			public byte[] GetCPUData()
			{
				return data;
			}

			public void CopyCPUToGPU(int vertexSize, int offset, int size)
			{
				vertexSize = FilterVertexSize(vertexSize);

				var cb = Get(vertexSize);

				cb.SetData(data, offset, offset, size);
			}

			public ComputeBuffer Get(int vertexSize)
			{
				vertexSize = FilterVertexSize(vertexSize);

				if (!computeBuffers.ContainsKey(vertexSize))
				{
					var count = VertexMaxSize / vertexSize;
					if (count * vertexSize != VertexMaxSize) count++;
					computeBuffers.Add(vertexSize, new ComputeBuffer(count, vertexSize));
				}

				return computeBuffers[vertexSize];
			}

			public DelayEvent[] ReallocateComputeBuffers(int desiredSize)
			{
				while(desiredSize > VertexMaxSize)
				{
					VertexMaxSize *= 2;
				}

#if EFFEKSEER_INTERNAL_DEBUG
				Debug.Log("ComputeBufferCollection : ReallocateComputeBuffers : " + (VertexMaxSize).ToString());
#endif
				List<DelayEvent> events = new List<DelayEvent>();
				foreach (var computeBuffer in computeBuffers)
				{
					events.Add(new DelayEventDisposeComputeBuffer(computeBuffer.Value));
				}

				var newComputeBuffers = new Dictionary<int, ComputeBuffer>();

				foreach(var cb in computeBuffers)
				{
					var count = VertexMaxSize / cb.Key;
					if (count * cb.Key != VertexMaxSize) count++;
					newComputeBuffers.Add(cb.Key, new ComputeBuffer(count, cb.Key));
				}

				computeBuffers = newComputeBuffers;
				data = new byte[VertexMaxSize];

				return events.ToArray();
			}

			public void Dispose()
			{
#if EFFEKSEER_INTERNAL_DEBUG
				Debug.Log("ComputeBufferCollection : Dispose");
#endif

				foreach (var computeBuffer in computeBuffers)
				{
					computeBuffer.Value.Release();
				}
				computeBuffers.Clear();
			}

			int FilterVertexSize(int size)
			{
#if UNITY_PS4
				return size;
#else
				return defaultVertexSize;
#endif
			}
		}

		private class RenderPath : IDisposable
		{
			public Camera camera;
			public CommandBuffer commandBuffer;
			public bool isCommandBufferFromExternal = false;
			public CameraEvent cameraEvent;
			public int renderId;
			public BackgroundRenderTexture renderTexture;
			public ComputeBufferCollection computeBufferFront;
			public ComputeBufferCollection computeBufferBack;
			public int LifeTime = 5;

			bool isDistortionEnabled = false;

			public MaterialPropCollection materiaProps = null;
			public ModelBufferCollection modelBuffers = null;
			public CustomDataBufferCollection customDataBuffers = null;

			List<DelayEvent> delayEvents = null;
			
			public RenderPath(Camera camera, CameraEvent cameraEvent, int renderId, bool isCommandBufferFromExternal)
			{
				this.camera = camera;
				this.renderId = renderId;
				this.cameraEvent = cameraEvent;
				this.delayEvents = new List<DelayEvent>();
				this.isCommandBufferFromExternal = isCommandBufferFromExternal;
				materiaProps = new MaterialPropCollection();
				modelBuffers = new ModelBufferCollection();
				customDataBuffers = new CustomDataBufferCollection();
			}

			private void SetupBackgroundBuffer(bool enableDistortion, RenderTargetProperty renderTargetProperty)
			{
				if (this.renderTexture != null)
				{
					this.renderTexture.Release();
					this.renderTexture = null;
				}

				if (enableDistortion)
				{
					var targetSize = BackgroundRenderTexture.GetRequiredSize(this.camera, renderTargetProperty);

#if UNITY_IOS || UNITY_ANDROID
					RenderTextureFormat format = RenderTextureFormat.ARGB32;
#else
					RenderTextureFormat format = (this.camera.allowHDR) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#endif
					this.renderTexture = new BackgroundRenderTexture(targetSize.x, targetSize.y, 0, format, renderTargetProperty);

					// HACK for ZenPhone
					if (this.renderTexture == null || !this.renderTexture.Create())
					{
						this.renderTexture = null;
					}
				}
			}

			public void Init(bool enableDistortion, RenderTargetProperty renderTargetProperty)
			{
				isDistortionEnabled = enableDistortion;

				if(enableDistortion && renderTargetProperty != null && renderTargetProperty.colorTargetDescriptor.msaaSamples > 1)
				{
					Debug.LogWarning("Distortion with MSAA is differnt from Editor on [Effekseer] Effekseer(*RP)");
					Debug.LogWarning("If LWRP or URP, please check Opacue Texture is PipelineAsset");
				}

				SetupBackgroundBuffer(isDistortionEnabled, renderTargetProperty);

				// Create a command buffer that is effekseer renderer
				if(!isCommandBufferFromExternal)
				{
					this.commandBuffer = new CommandBuffer();
					this.commandBuffer.name = "Effekseer Rendering";

					// register the command to a camera
					this.camera.AddCommandBuffer(this.cameraEvent, this.commandBuffer);
				}

				computeBufferFront = new ComputeBufferCollection();
				computeBufferBack = new ComputeBufferCollection();
			}

			public void ReallocateComputeBuffer(int desiredSize)
			{
				delayEvents.AddRange(computeBufferFront.ReallocateComputeBuffers(desiredSize));
				delayEvents.AddRange(computeBufferBack.ReallocateComputeBuffers(desiredSize));
			}

			public void Dispose()
			{
				if (this.commandBuffer != null && !isCommandBufferFromExternal)
				{
					if (this.camera != null)
					{
						this.camera.RemoveCommandBuffer(this.cameraEvent, this.commandBuffer);
					}
					this.commandBuffer.Dispose();
					this.commandBuffer = null;
				}

				if (this.computeBufferFront != null)
				{
					this.computeBufferFront.Dispose();
					this.computeBufferFront = null;
				}

				if (this.computeBufferBack != null)
				{
					this.computeBufferBack.Dispose();
					this.computeBufferBack = null;
				}

				if (this.renderTexture != null)
				{
					this.renderTexture.Release();
					this.renderTexture = null;
				}

				if (this.modelBuffers != null)
				{
					this.modelBuffers.Release();
				}

				if (this.customDataBuffers != null)
				{
					this.customDataBuffers.Release();
				}

				foreach (var e in delayEvents)
				{
					e.Call();
				}
				delayEvents.Clear();
			}

			public bool IsValid(RenderTargetProperty renderTargetProperty)
			{
				if (this.isDistortionEnabled != EffekseerRendererUtils.IsDistortionEnabled) return false;

				if (this.renderTexture != null)
				{
					var targetSize = BackgroundRenderTexture.GetRequiredSize(this.camera, renderTargetProperty);

					return targetSize.x == this.renderTexture.width &&
						targetSize.y == this.renderTexture.height;
				}
				return true;
			}

			public void Update()
			{
				foreach(var e in delayEvents)
				{
					e.RestTime--;
					if(e.RestTime <= 0)
					{
						e.Call();
					}
				}

				delayEvents.RemoveAll(_ => _.RestTime <= 0);
			}

			public void AssignExternalCommandBuffer(CommandBuffer commandBuffer)
			{
				if(!isCommandBufferFromExternal)
				{
					Debug.LogError("External command buffer is assigned even if isCommandBufferFromExternal is true.");
				}

				this.commandBuffer = commandBuffer;
			}

			public void ResetBuffers()
			{
				if(!isCommandBufferFromExternal)
				{
					commandBuffer.Clear();
				}

				materiaProps.Reset();
				modelBuffers.Reset();
				customDataBuffers.Reset();
			}
		};

		MaterialCollection materials = new MaterialCollection();
		MaterialCollection materialsDistortion = new MaterialCollection();
		MaterialCollection materialsLighting = new MaterialCollection();
		MaterialCollection materialsModel = new MaterialCollection();
		MaterialCollection materialsModelDistortion = new MaterialCollection();
		MaterialCollection materialsModelLighting = new MaterialCollection();
		int nextRenderID = 0;

		public EffekseerRendererUnity()
		{
			materials.Shader = EffekseerSettings.Instance.standardShader;
			materialsDistortion.Shader = EffekseerSettings.Instance.standardDistortionShader;
			materialsLighting.Shader = EffekseerSettings.Instance.standardLightingShader;
			materialsModel.Shader = EffekseerSettings.Instance.standardModelShader;
			materialsModelDistortion.Shader = EffekseerSettings.Instance.standardModelDistortionShader;
			materialsModelLighting.Shader = EffekseerSettings.Instance.standardLightingShader;
			materialsModelLighting.Keywords = new string[] { "_MODEL_" };
		}

		// RenderPath per Camera
		private Dictionary<Camera, RenderPath> renderPaths = new Dictionary<Camera, RenderPath>();

		public int layer { get; set; }

		public void SetVisible(bool visible)
		{
			if (visible)
			{
				Camera.onPreCull += Render;
				Camera.onPostRender += OnPostRender;
			}
			else
			{
				Camera.onPreCull -= Render;
				Camera.onPostRender -= OnPostRender;
			}
		}

		public void CleanUp()
		{
			// dispose all render pathes
			foreach (var pair in renderPaths)
			{
				pair.Value.Dispose();
				Plugin.EffekseerAddRemovingRenderPath(pair.Value.renderId);
			}
			renderPaths.Clear();
		}

		public CommandBuffer GetCameraCommandBuffer(Camera camera)
		{
			if (renderPaths.ContainsKey(camera))
			{
				return renderPaths[camera].commandBuffer;
			}
			return null;
		}

		public void Render(Camera camera)
		{
			Render(camera, null, null);
		}

		public void Render(Camera camera, RenderTargetProperty renderTargetProperty, CommandBuffer targetCommandBuffer)
		{
			var settings = EffekseerSettings.Instance;

#if UNITY_EDITOR
			if (camera.cameraType == CameraType.SceneView)
			{
				// check a camera in the scene view
				if (settings.drawInSceneView == false)
				{
					return;
				}
			}
#endif
			// check a culling mask
			var mask = Effekseer.Plugin.EffekseerGetCameraCullingMaskToShowAllEffects();

			// don't need to update because doesn't exists and need not to render
			if ((camera.cullingMask & mask) == 0 && !renderPaths.ContainsKey(camera))
			{
				if(renderPaths.ContainsKey(camera))
				{
					renderPaths[camera].ResetBuffers();
				}
				return;
			}

			// GC renderpaths
			bool hasDisposed = false;
			foreach (var path_ in renderPaths)
			{
				path_.Value.LifeTime--;
				if (path_.Value.LifeTime < 0)
				{
					path_.Value.Dispose();
					hasDisposed = true;
				}
			}

			// dispose renderpaths
			if (hasDisposed)
			{
				List<Camera> removed = new List<Camera>();
				foreach (var path_ in renderPaths)
				{
					if (path_.Value.LifeTime >= 0) continue;

					removed.Add(path_.Key);
					Plugin.EffekseerAddRemovingRenderPath(path_.Value.renderId);
				}

				foreach (var r in removed)
				{
					renderPaths.Remove(r);
				}
			}

			RenderPath path;

			if (renderPaths.ContainsKey(camera))
			{
				path = renderPaths[camera];
			}
			else
			{
				// render path doesn't exists, create a render path
				while (true)
				{
					bool found = false;
					foreach (var kv in renderPaths)
					{
						if (kv.Value.renderId == nextRenderID)
						{
							found = true;
							break;
						}
					}

					if (found)
					{
						nextRenderID++;
					}
					else
					{
						break;
					}
				}

				path = new RenderPath(camera, cameraEvent, nextRenderID, targetCommandBuffer != null);
				path.Init(EffekseerRendererUtils.IsDistortionEnabled, renderTargetProperty);
				renderPaths.Add(camera, path);
				nextRenderID = (nextRenderID + 1) % EffekseerRendererUtils.RenderIDCount;
			}

			if (!path.IsValid(renderTargetProperty))
			{
				path.Dispose();
				path.Init(EffekseerRendererUtils.IsDistortionEnabled, renderTargetProperty);
			}

			path.Update();
			path.LifeTime = 60;
			Plugin.EffekseerSetRenderingCameraCullingMask(path.renderId, camera.cullingMask);

			// effects shown don't exists
			if ((camera.cullingMask & mask) == 0)
			{
				path.ResetBuffers();
				return;
			}

			if(path.isCommandBufferFromExternal)
			{
				path.AssignExternalCommandBuffer(targetCommandBuffer);
			}

			// assign a dinsotrion texture
			if (path.renderTexture != null)
			{
				Plugin.EffekseerSetBackGroundTexture(path.renderId, path.renderTexture.ptr);
			}
			else
			{
				Plugin.EffekseerSetBackGroundTexture(path.renderId, IntPtr.Zero);
			}

			// update view matrixes
			Plugin.EffekseerSetProjectionMatrix(path.renderId, Utility.Matrix2Array(
				GL.GetGPUProjectionMatrix(camera.projectionMatrix, false)));
			Plugin.EffekseerSetCameraMatrix(path.renderId, Utility.Matrix2Array(
				camera.worldToCameraMatrix));

			// Reset command buffer
			path.ResetBuffers();

			// generate render events on this thread
			Plugin.EffekseerRenderBack(path.renderId);

			// if memory is lacked, reallocate memory
			while(Plugin.GetUnityRenderParameterCount() > 0 && Plugin.GetUnityRenderVertexBufferCount() > path.computeBufferBack.GetCPUData().Length)
			{
				path.ReallocateComputeBuffer(Plugin.GetUnityRenderVertexBufferCount());
			}

			RenderInternal(path.commandBuffer, path.computeBufferBack, path.materiaProps, path.modelBuffers, path.customDataBuffers, path.renderTexture);

			// Distortion
			if (EffekseerRendererUtils.IsDistortionEnabled && 
				(path.renderTexture != null || renderTargetProperty != null))
			{
				// Add a blit command that copy to the distortion texture
				if(renderTargetProperty != null && renderTargetProperty.colorBufferID.HasValue)
				{
					path.commandBuffer.Blit(renderTargetProperty.colorBufferID.Value, path.renderTexture.renderTexture);
					path.commandBuffer.SetRenderTarget(renderTargetProperty.colorBufferID.Value);

					if (renderTargetProperty.Viewport.width > 0)
					{
						path.commandBuffer.SetViewport(renderTargetProperty.Viewport);
					}
				}
                else if (renderTargetProperty != null)
                {
					renderTargetProperty.ApplyToCommandBuffer(path.commandBuffer, path.renderTexture);

					if (renderTargetProperty.Viewport.width > 0)
					{
						path.commandBuffer.SetViewport(renderTargetProperty.Viewport);
					}
				}
				else
				{
					path.commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, path.renderTexture.renderTexture);
					path.commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
				}
			}

			Plugin.EffekseerRenderFront(path.renderId);

			// if memory is lacked, reallocate memory
			while (Plugin.GetUnityRenderParameterCount() > 0 && Plugin.GetUnityRenderVertexBufferCount() > path.computeBufferFront.GetCPUData().Length)
			{
				path.ReallocateComputeBuffer(Plugin.GetUnityRenderVertexBufferCount());
			}

			RenderInternal(path.commandBuffer, path.computeBufferFront, path.materiaProps, path.modelBuffers, path.customDataBuffers, path.renderTexture);
		}

		Texture GetCachedTexture(IntPtr key, BackgroundRenderTexture background, DummyTextureType type)
		{
			if (background != null && background.ptr == key) return background.renderTexture;

			return EffekseerSystem.GetCachedTexture(key, type);
		}

		unsafe void RenderInternal(CommandBuffer commandBuffer, ComputeBufferCollection computeBuffer, MaterialPropCollection matPropCol, ModelBufferCollection modelBufferCol, CustomDataBufferCollection customDataBufferCol, BackgroundRenderTexture background)
		{
			var renderParameterCount = Plugin.GetUnityRenderParameterCount();
			// var vertexBufferSize = Plugin.GetUnityRenderVertexBufferCount();

			if (renderParameterCount > 0)
			{
				Plugin.UnityRenderParameter parameter = new Plugin.UnityRenderParameter();

#if !UNITY_PS4
				var vertexBufferCount = Plugin.GetUnityRenderVertexBufferCount();

				if(vertexBufferCount > 0)
				{
					var vertexBuffer = Plugin.GetUnityRenderVertexBuffer();

					Marshal.Copy(vertexBuffer, computeBuffer.GetCPUData(), 0, vertexBufferCount);
					computeBuffer.CopyCPUToGPU(VertexSize, 0, vertexBufferCount);
				}
#endif
				var infoBuffer = Plugin.GetUnityRenderInfoBuffer();

				for (int i = 0; i < renderParameterCount; i++)
				{
					Plugin.GetUnityRenderParameter(ref parameter, i);
					
					if(parameter.RenderMode == 1)
					{
						RenderModdel(parameter, infoBuffer, commandBuffer, matPropCol, modelBufferCol, customDataBufferCol, background);
					}
					else
					{
						RenderSprite(parameter, infoBuffer, commandBuffer, computeBuffer, matPropCol, background);
					}
				}
			}

		}

		unsafe void RenderSprite(Plugin.UnityRenderParameter parameter, IntPtr infoBuffer, CommandBuffer commandBuffer, ComputeBufferCollection computeBuffer, MaterialPropCollection matPropCol, BackgroundRenderTexture background)
		{
			var prop = matPropCol.GetNext();

			MaterialKey key = new MaterialKey();
			key.Blend = (AlphaBlendType)parameter.Blend;
			key.ZTest = parameter.ZTest > 0;
			key.ZWrite = parameter.ZWrite > 0;
			key.Cull = (int)UnityEngine.Rendering.CullMode.Off;

#if UNITY_PS4
			{
				var vertexBuffer = (byte*)Plugin.GetUnityRenderVertexBuffer();
				vertexBuffer += parameter.VertexBufferOffset;
				Marshal.Copy(new IntPtr(vertexBuffer), computeBuffer.GetCPUData(), parameter.VertexBufferOffset, parameter.ElementCount * 4 * parameter.VertexBufferStride);
			}

			computeBuffer.CopyCPUToGPU(parameter.VertexBufferStride, parameter.VertexBufferOffset, parameter.ElementCount * 4 * parameter.VertexBufferStride);
#endif
			prop.SetFloat("buf_offset", parameter.VertexBufferOffset / parameter.VertexBufferStride);
			prop.SetBuffer("buf_vertex", computeBuffer.Get(parameter.VertexBufferStride));

			if (parameter.MaterialType == Plugin.RendererMaterialType.File)
			{
				var efkMaterial = EffekseerSystem.GetCachedMaterial(parameter.MaterialPtr);
				if (efkMaterial == null)
				{
					return;
				}
				Material material = null;

				if(parameter.IsRefraction > 0)
				{
					if(efkMaterial.materialsRefraction == null)
					{
						return;
					}

					material = efkMaterial.materialsRefraction.GetMaterial(ref key);
				}
				else
				{
					material = efkMaterial.materials.GetMaterial(ref key);
				}

				prop.SetVector("lightDirection", EffekseerSystem.LightDirection.normalized);
				prop.SetColor("lightColor", EffekseerSystem.LightColor);
				prop.SetColor("lightAmbientColor", EffekseerSystem.LightAmbientColor);

				for (int ti = 0; ti < efkMaterial.asset.textures.Length; ti++)
				{
					var ptr = parameter.GetTexturePtr(ti);
					var texture = GetCachedTexture(ptr, background, DummyTextureType.White);
					if (texture != null)
					{
						prop.SetTexture(efkMaterial.asset.textures[ti].Name, texture);
						texture.wrapMode = TextureWrapMode.Repeat;
						texture.filterMode = FilterMode.Bilinear;
					}
				}

				for (int ui = 0; ui < efkMaterial.asset.uniforms.Length; ui++)
				{
					var f = ((float*)(((byte*)infoBuffer.ToPointer()) + parameter.UniformBufferOffset));
					prop.SetVector(efkMaterial.asset.uniforms[ui].Name, new Vector4(f[ui * 4 + 0], f[ui * 4 + 1], f[ui * 4 + 2], f[ui * 4 + 3]));
				}

				if (parameter.IsRefraction > 0 && background != null)
				{
					prop.SetTexture("_BackTex", GetCachedTexture(parameter.GetTexturePtr(efkMaterial.asset.textures.Length), background, DummyTextureType.White));
				}

				commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
			}
			else if (parameter.MaterialType == Plugin.RendererMaterialType.Lighting)
			{
				var material = materialsLighting.GetMaterial(ref key);

				prop.SetVector("lightDirection", EffekseerSystem.LightDirection.normalized);
				prop.SetColor("lightColor", EffekseerSystem.LightColor);
				prop.SetColor("lightAmbientColor", EffekseerSystem.LightAmbientColor);

				var colorTexture = GetCachedTexture(parameter.TexturePtrs0, background, DummyTextureType.White);
				var normalTexture = GetCachedTexture(parameter.TexturePtrs1, background, DummyTextureType.Normal);
				if (parameter.TextureWrapTypes[0] == 0)
				{
					colorTexture.wrapMode = TextureWrapMode.Repeat;
				}
				else
				{
					colorTexture.wrapMode = TextureWrapMode.Clamp;
				}

				if (parameter.TextureFilterTypes[0] == 0)
				{
					colorTexture.filterMode = FilterMode.Point;
				}
				else
				{
					colorTexture.filterMode = FilterMode.Bilinear;
				}

				if (parameter.TextureWrapTypes[1] == 0)
				{
					normalTexture.wrapMode = TextureWrapMode.Repeat;
				}
				else
				{
					normalTexture.wrapMode = TextureWrapMode.Clamp;
				}

				if (parameter.TextureFilterTypes[1] == 0)
				{
					normalTexture.filterMode = FilterMode.Point;
				}
				else
				{
					normalTexture.filterMode = FilterMode.Bilinear;
				}
				
				prop.SetTexture("_ColorTex", colorTexture);
				prop.SetTexture("_NormalTex", normalTexture);

				commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
			}
			else if (parameter.MaterialType == Plugin.RendererMaterialType.BackDistortion)
			{
				var material = materialsDistortion.GetMaterial(ref key);

				prop.SetFloat("distortionIntensity", parameter.DistortionIntensity);

				var colorTexture = GetCachedTexture(parameter.TexturePtrs0, background, DummyTextureType.White);
				if (parameter.TextureWrapTypes[0] == 0)
				{
					colorTexture.wrapMode = TextureWrapMode.Repeat;
				}
				else
				{
					colorTexture.wrapMode = TextureWrapMode.Clamp;
				}

				if (parameter.TextureFilterTypes[0] == 0)
				{
					colorTexture.filterMode = FilterMode.Point;
				}
				else
				{
					colorTexture.filterMode = FilterMode.Bilinear;
				}

				prop.SetTexture("_ColorTex", colorTexture);

				if (background != null)
				{
					prop.SetTexture("_BackTex", GetCachedTexture(parameter.TexturePtrs1, background, DummyTextureType.White));
					commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
				}
			}
			else
			{
				var material = materials.GetMaterial(ref key);

				var colorTexture = GetCachedTexture(parameter.TexturePtrs0, background, DummyTextureType.White);
				if (parameter.TextureWrapTypes[0] == 0)
				{
					colorTexture.wrapMode = TextureWrapMode.Repeat;
				}
				else
				{
					colorTexture.wrapMode = TextureWrapMode.Clamp;
				}

				if (parameter.TextureFilterTypes[0] == 0)
				{
					colorTexture.filterMode = FilterMode.Point;
				}
				else
				{
					colorTexture.filterMode = FilterMode.Bilinear;
				}

				prop.SetTexture("_ColorTex", colorTexture);

				commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
			}
		}

		unsafe void RenderModdel(Plugin.UnityRenderParameter parameter, IntPtr infoBuffer, CommandBuffer commandBuffer, MaterialPropCollection matPropCol, ModelBufferCollection modelBufferCol, CustomDataBufferCollection customDataBuffers, BackgroundRenderTexture background)
		{
			// Draw model
			var modelParameters = ((Plugin.UnityRenderModelParameter*)(((byte*)infoBuffer.ToPointer()) + parameter.VertexBufferOffset));

			MaterialKey key = new MaterialKey();
			key.Blend = (AlphaBlendType)parameter.Blend;
			key.ZTest = parameter.ZTest > 0;
			key.ZWrite = parameter.ZWrite > 0;

			if (parameter.Culling == 0)
			{
				key.Cull = (int)UnityEngine.Rendering.CullMode.Back;
			}
			if (parameter.Culling == 1)
			{
				key.Cull = (int)UnityEngine.Rendering.CullMode.Front;
			}
			if (parameter.Culling == 2)
			{
				key.Cull = (int)UnityEngine.Rendering.CullMode.Off;
			}

			var model = EffekseerSystem.GetCachedModel(parameter.ModelPtr);
			if (model == null)
				return;

			var count = parameter.ElementCount;
			var offset = 0;

			while (count > 0)
			{
				var prop = matPropCol.GetNext();
				ComputeBuffer computeBuf = null;
				var allocated = modelBufferCol.Allocate(modelParameters, offset, count, ref computeBuf);

				if (parameter.MaterialType == Plugin.RendererMaterialType.File)
				{
					var efkMaterial = EffekseerSystem.GetCachedMaterial(parameter.MaterialPtr);
					if (efkMaterial == null)
					{
						offset += allocated;
						count -= allocated;
					}

					Material material = null;

					if (parameter.IsRefraction > 0)
					{
						material = efkMaterial.materialsModelRefraction.GetMaterial(ref key);
					}
					else
					{
						material = efkMaterial.materialsModel.GetMaterial(ref key);
					}

					prop.SetBuffer("buf_vertex", model.VertexBuffer);
					prop.SetBuffer("buf_index", model.IndexBuffer);
					prop.SetBuffer("buf_vertex_offsets", model.VertexOffsets);
					prop.SetBuffer("buf_index_offsets", model.IndexOffsets);
					prop.SetBuffer("buf_model_parameter", computeBuf);

					prop.SetVector("lightDirection", EffekseerSystem.LightDirection.normalized);
					prop.SetColor("lightColor", EffekseerSystem.LightColor);
					prop.SetColor("lightAmbientColor", EffekseerSystem.LightAmbientColor);

					for (int ti = 0; ti < efkMaterial.asset.textures.Length; ti++)
					{
						var ptr = parameter.GetTexturePtr(ti);
						var texture = GetCachedTexture(ptr, background, DummyTextureType.White);
						if (texture != null)
						{
							prop.SetTexture(efkMaterial.asset.textures[ti].Name, texture);
							texture.wrapMode = TextureWrapMode.Repeat;
							texture.filterMode = FilterMode.Bilinear;
						}
					}

					for (int ui = 0; ui < efkMaterial.asset.uniforms.Length; ui++)
					{
						var f = ((float*)(((byte*)infoBuffer.ToPointer()) + parameter.UniformBufferOffset));
						var uniform = new Vector4(f[ui * 4 + 0], f[ui * 4 + 1], f[ui * 4 + 2], f[ui * 4 + 3]);
						prop.SetVector(efkMaterial.asset.uniforms[ui].Name, uniform);
					}

					// CustomData
					if(efkMaterial.asset.CustomData1Count > 0)
					{
						ComputeBuffer cb = null;
						var all = customDataBuffers.Allocate((CustomDataBuffer*)((byte*)infoBuffer.ToPointer() + parameter.CustomData1BufferOffset), offset, count, ref cb);
						if (all != allocated) throw new Exception();
						prop.SetBuffer("buf_customData1", cb);
					}

					if (efkMaterial.asset.CustomData2Count > 0)
					{
						ComputeBuffer cb = null;
						var all = customDataBuffers.Allocate((CustomDataBuffer*)((byte*)infoBuffer.ToPointer() + parameter.CustomData2BufferOffset), offset, count, ref cb);
						if (all != allocated) throw new Exception();
						prop.SetBuffer("buf_customData2", cb);
					}

					if (parameter.IsRefraction > 0 && background != null)
					{
						prop.SetTexture("_BackTex", GetCachedTexture(parameter.GetTexturePtr(efkMaterial.asset.textures.Length), background, DummyTextureType.White));
					}

					commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, model.IndexCounts[0], allocated, prop);
				}
				else if (parameter.MaterialType == Plugin.RendererMaterialType.Lighting)
				{
					var material = materialsModelLighting.GetMaterial(ref key);

					prop.SetBuffer("buf_vertex", model.VertexBuffer);
					prop.SetBuffer("buf_index", model.IndexBuffer);
					prop.SetBuffer("buf_vertex_offsets", model.VertexOffsets);
					prop.SetBuffer("buf_index_offsets", model.IndexOffsets);
					prop.SetBuffer("buf_model_parameter", computeBuf);

					prop.SetVector("lightDirection", EffekseerSystem.LightDirection.normalized);
					prop.SetColor("lightColor", EffekseerSystem.LightColor);
					prop.SetColor("lightAmbientColor", EffekseerSystem.LightAmbientColor);

					var colorTexture = GetCachedTexture(parameter.TexturePtrs0, background, DummyTextureType.White);
					var normalTexture = GetCachedTexture(parameter.TexturePtrs1, background, DummyTextureType.Normal);
					if (parameter.TextureWrapTypes[0] == 0)
					{
						colorTexture.wrapMode = TextureWrapMode.Repeat;
					}
					else
					{
						colorTexture.wrapMode = TextureWrapMode.Clamp;
					}

					if (parameter.TextureFilterTypes[0] == 0)
					{
						colorTexture.filterMode = FilterMode.Point;
					}
					else
					{
						colorTexture.filterMode = FilterMode.Bilinear;
					}

					if (parameter.TextureWrapTypes[1] == 0)
					{
						normalTexture.wrapMode = TextureWrapMode.Repeat;
					}
					else
					{
						normalTexture.wrapMode = TextureWrapMode.Clamp;
					}

					if (parameter.TextureFilterTypes[1] == 0)
					{
						normalTexture.filterMode = FilterMode.Point;
					}
					else
					{
						normalTexture.filterMode = FilterMode.Bilinear;
					}

					prop.SetTexture("_ColorTex", colorTexture);
					prop.SetTexture("_NormalTex", normalTexture);

					commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, model.IndexCounts[0], allocated, prop);
				}
				else if (parameter.MaterialType == Plugin.RendererMaterialType.BackDistortion)
				{
					var material = materialsModelDistortion.GetMaterial(ref key);

					prop.SetBuffer("buf_vertex", model.VertexBuffer);
					prop.SetBuffer("buf_index", model.IndexBuffer);
					prop.SetBuffer("buf_vertex_offsets", model.VertexOffsets);
					prop.SetBuffer("buf_index_offsets", model.IndexOffsets);
					prop.SetBuffer("buf_model_parameter", computeBuf);

					var colorTexture = GetCachedTexture(parameter.TexturePtrs0, background, DummyTextureType.White);
					if (parameter.TextureWrapTypes[0] == 0)
					{
						colorTexture.wrapMode = TextureWrapMode.Repeat;
					}
					else
					{
						colorTexture.wrapMode = TextureWrapMode.Clamp;
					}

					if (parameter.TextureFilterTypes[0] == 0)
					{
						colorTexture.filterMode = FilterMode.Point;
					}
					else
					{
						colorTexture.filterMode = FilterMode.Bilinear;
					}

					prop.SetTexture("_ColorTex", colorTexture);

					if (background != null)
					{
						prop.SetTexture("_BackTex", GetCachedTexture(parameter.TexturePtrs1, background, DummyTextureType.White));
						//Temp
						//prop.SetTexture("_BackTex", background);

						commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, model.IndexCounts[0], allocated, prop);
					}
				}
				else
				{
					var material = materialsModel.GetMaterial(ref key);

					prop.SetBuffer("buf_vertex", model.VertexBuffer);
					prop.SetBuffer("buf_index", model.IndexBuffer);
					prop.SetBuffer("buf_vertex_offsets", model.VertexOffsets);
					prop.SetBuffer("buf_index_offsets", model.IndexOffsets);
					prop.SetBuffer("buf_model_parameter", computeBuf);

					var colorTexture = GetCachedTexture(parameter.TexturePtrs0, background, DummyTextureType.White);
					if (parameter.TextureWrapTypes[0] == 0)
					{
						colorTexture.wrapMode = TextureWrapMode.Repeat;
					}
					else
					{
						colorTexture.wrapMode = TextureWrapMode.Clamp;
					}

					if (parameter.TextureFilterTypes[0] == 0)
					{
						colorTexture.filterMode = FilterMode.Point;
					}
					else
					{
						colorTexture.filterMode = FilterMode.Bilinear;
					}

					prop.SetTexture("_ColorTex", colorTexture);

					commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, model.IndexCounts[0], allocated, prop);
				}

				offset += allocated;
				count -= allocated;
			}
		}

		public void OnPostRender(Camera camera)
		{
			if (renderPaths.ContainsKey(camera))
			{
				RenderPath path = renderPaths[camera];
				Plugin.EffekseerSetRenderSettings(path.renderId,
					(camera.activeTexture != null));
			}
		}
	}
}