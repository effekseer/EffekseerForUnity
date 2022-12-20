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
				((ZTest ? 1 : 0) << 4) +
				((ZWrite ? 1 : 0) << 5) +
				(Cull << 6);
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

			foreach (var keyword in Keywords)
			{
				material.EnableKeyword(keyword);
			}

			materials.Add(id, material);

#if UNITY_EDITOR
			UnityEditor.ShaderUtil.CompilePass(material, 0, true);
#endif

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

			if (asset.HasRefraction)
			{
				materialsRefraction = new MaterialCollection();
				materialsModelRefraction = new MaterialCollection();
				materialsRefraction.Shader = asset.shader;
				materialsRefraction.Keywords = new string[] { "_MATERIAL_REFRACTION_" };
				materialsModelRefraction.Shader = asset.shader;
				materialsModelRefraction.Keywords = new string[] { "_MODEL_", "_MATERIAL_REFRACTION_" };
			}
		}

		public bool IsValid
		{
			get { return asset != null && asset.shader != null; }
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

		public unsafe void Initialize(Plugin.ModelVertex* vertecies,
															int verteciesCount,
															Plugin.ModelFace* faces,
															int facesCount)
		{
			List<Vertex> vertexBuffer = new List<Vertex>();
			List<int> indexBuffer = new List<int>();

			for (int i = 0; i < verteciesCount; i++)
			{
				vertexBuffer.Add(
					new Vertex
					{
						Position = vertecies[i].Position,
						Normal = vertecies[i].Normal,
						Binormal = vertecies[i].Binormal,
						Tangent = vertecies[i].Tangent,
						UV = vertecies[i].UV,
						VColor = new Color(vertecies[i].VColor.r / 255.0f, vertecies[i].VColor.g / 255.0f, vertecies[i].VColor.b / 255.0f, vertecies[i].VColor.a / 255.0f)
					});
			}

			for (int i = 0; i < facesCount; i++)
			{
				indexBuffer.Add(faces[i].Index1);
				indexBuffer.Add(faces[i].Index2);
				indexBuffer.Add(faces[i].Index3);
			}

			VertexBuffer = new ComputeBuffer(verteciesCount, sizeof(Vertex));
			IndexBuffer = new ComputeBuffer(facesCount * 3, sizeof(int));

			VertexBuffer.SetData(vertexBuffer, 0, 0, vertexBuffer.Count);
			IndexBuffer.SetData(indexBuffer, 0, 0, indexBuffer.Count);

			IndexCounts.Add(facesCount * 3);

			vertexOffsets.Add(0);
			indexOffsets.Add(0);

			VertexOffsets = new ComputeBuffer(vertexOffsets.Count, sizeof(int));
			IndexOffsets = new ComputeBuffer(indexOffsets.Count, sizeof(int));
			VertexOffsets.SetData(vertexOffsets);
			IndexOffsets.SetData(indexOffsets);
		}

		public unsafe void Initialize(byte[] buffer)
		{
			int sizeEffekseerVertex = 4 * 15;

			int version = 0;
			int offset = 0;
			version = BitConverter.ToInt32(buffer, offset);
			offset += sizeof(int);

			if (version < 1)
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

				if (version < 1)
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
		private StandardBlitter standardBlitter = new StandardBlitter();

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

			class Block
			{
				public ComputeBuffer gpuBuf1;
				public Plugin.UnityRenderModelParameter1[] cpuBuf1;
				public ComputeBuffer gpuBuf2;
				public Plugin.UnityRenderModelParameter2[] cpuBuf2;

				public unsafe void Init()
				{
					gpuBuf1 = new ComputeBuffer(elementCount, sizeof(Plugin.UnityRenderModelParameter1));
					cpuBuf1 = new Plugin.UnityRenderModelParameter1[elementCount];

					gpuBuf2 = new ComputeBuffer(elementCount, sizeof(Plugin.UnityRenderModelParameter2));
					cpuBuf2 = new Plugin.UnityRenderModelParameter2[elementCount];
				}

				public void Dispose()
				{
					if (gpuBuf1 != null)
					{
						gpuBuf1.Release();
						gpuBuf1 = null;
					}

					if (gpuBuf2 != null)
					{
						gpuBuf2.Release();
						gpuBuf2 = null;
					}
				}
			}

			List<Block> blocks = new List<Block>();

			int bufferOffset = 0;

			public unsafe ModelBufferCollection()
			{
				for (int i = 0; i < 10; i++)
				{
					var block = new Block();
					block.Init();
					blocks.Add(block);
				}
			}

			public void Reset()
			{
				bufferOffset = 0;
			}

			public unsafe int Allocate(Plugin.UnityRenderModelParameter1* param1, Plugin.UnityRenderModelParameter2* param2, int offset, int count, ref ComputeBuffer computeBuffer1, ref ComputeBuffer computeBuffer2)
			{
				if (bufferOffset >= blocks.Count)
				{
					var newBlock = new Block();
					newBlock.Init();
					blocks.Add(newBlock);
				}

				var block = blocks[bufferOffset];
				bufferOffset++;

				if (count >= elementCount)
				{
					count = elementCount;
				}

				for (int i = 0; i < count; i++)
				{
					block.cpuBuf1[i] = param1[offset + i];
					block.cpuBuf2[i] = param2[offset + i];
				}

				block.gpuBuf1.SetData(block.cpuBuf1, 0, 0, count);
				block.gpuBuf2.SetData(block.cpuBuf2, 0, 0, count);

				computeBuffer1 = block.gpuBuf1;
				computeBuffer2 = block.gpuBuf2;

				return count;
			}

			public void Release()
			{
				for (int i = 0; i < blocks.Count; i++)
				{
					blocks[i].Dispose();
				}
				blocks.Clear();
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
				if (cb != null)
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

			const int defaultStride = 36;

			Dictionary<int, ComputeBuffer> computeBuffers = new Dictionary<int, ComputeBuffer>();
			byte[] data = null;

			public ComputeBufferCollection()
			{
				data = new byte[VertexMaxSize];
				Get(defaultStride, true);
			}

			public byte[] GetCPUData()
			{
				return data;
			}

			public void CopyCPUToGPU(int stride, int offset, int size)
			{
				var cb = Get(stride, true);
				cb.SetData(data, offset, offset, size);
			}

			public bool HasBuffer(int stride)
			{
				return computeBuffers.ContainsKey(stride);
			}
			public ComputeBuffer Get(int stride, bool rewuireToAllocate)
			{
				if (!HasBuffer(stride))
				{
					if (!rewuireToAllocate)
					{
						return null;
					}

					var count = VertexMaxSize / stride;
					if (count * stride != VertexMaxSize) count++;
					computeBuffers.Add(stride, new ComputeBuffer(count, stride));
				}

				return computeBuffers[stride];
			}

			public DelayEvent[] ReallocateComputeBuffers(int desiredSize)
			{
				while (desiredSize > VertexMaxSize)
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

				foreach (var cb in computeBuffers)
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
		}

		private class RenderPath : IDisposable
		{
			public Camera camera;
			public CommandBuffer commandBuffer;
			public bool isCommandBufferFromExternal = false;
			public CameraEvent cameraEvent;
			public int renderId;
			public BackgroundRenderTexture renderTexture;
			public DepthRenderTexture depthTexture;
			public ComputeBufferCollection computeBufferFront;
			public ComputeBufferCollection computeBufferBack;
			public int LifeTime = 5;

			bool isDistortionEnabled = false;
			bool isDepthEnabled = false;

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

			public void Init(bool enableDistortion, bool enableDepth, RenderTargetProperty renderTargetProperty)
			{
				isDistortionEnabled = enableDistortion;
				isDepthEnabled = enableDepth;

				RendererUtils.SetupBackgroundBuffer(ref renderTexture, isDistortionEnabled, camera, renderTargetProperty);
				RendererUtils.SetupDepthBuffer(ref depthTexture, isDepthEnabled, camera, renderTargetProperty);

				// Create a command buffer that is effekseer renderer
				if (!isCommandBufferFromExternal)
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

				if (this.depthTexture != null)
				{
					this.depthTexture.Release();
					this.depthTexture = null;
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
				if (this.isDepthEnabled != EffekseerRendererUtils.IsDepthEnabled) return false;

				if (depthTexture != null)
				{
					var targetSize = BackgroundRenderTexture.GetRequiredSize(this.camera, renderTargetProperty);

					if (targetSize.x != this.depthTexture.width ||
						targetSize.y != this.depthTexture.height)
					{
						return false;
					}
				}

				if (this.renderTexture != null)
				{
					var targetSize = BackgroundRenderTexture.GetRequiredSize(this.camera, renderTargetProperty);

					if (targetSize.x != this.renderTexture.width ||
						targetSize.y != this.renderTexture.height)
					{
						return false;
					}
				}

				return true;
			}

			public void Update()
			{
				foreach (var e in delayEvents)
				{
					e.RestTime--;
					if (e.RestTime <= 0)
					{
						e.Call();
					}
				}

				delayEvents.RemoveAll(_ => _.RestTime <= 0);
			}

			public void AssignExternalCommandBuffer(CommandBuffer commandBuffer)
			{
				if (!isCommandBufferFromExternal)
				{
					Debug.LogError("External command buffer is assigned even if isCommandBufferFromExternal is true.");
				}

				this.commandBuffer = commandBuffer;
			}

			public void ResetBuffers()
			{
				if (!isCommandBufferFromExternal)
				{
					commandBuffer.Clear();
				}

				materiaProps.Reset();
				modelBuffers.Reset();
				customDataBuffers.Reset();
			}
		};

		int nextRenderID = 0;

		Dictionary<int, MaterialCollection> materialCollections = new Dictionary<int, MaterialCollection>();

		MaterialCollection GetMaterialCollection(Plugin.RendererMaterialType type, bool isModel)
		{
			if (type == Plugin.RendererMaterialType.Material)
				return null;

			var key = ((int)type) * 2 + (isModel ? 1 : 0);

			materialCollections.TryGetValue(key, out var value);

			if (value != null)
			{
				return value;
			}

			value = new MaterialCollection();
			materialCollections.Add(key, value);
			return value;
		}

		public EffekseerRendererUnity()
		{
			var fixedShader = EffekseerDependentAssets.Instance.fixedShader;
#if UNITY_EDITOR
			if (fixedShader == null)
			{
				EffekseerDependentAssets.AssignAssets();
			}
			fixedShader = EffekseerDependentAssets.Instance.fixedShader;
#endif

			GetMaterialCollection(Plugin.RendererMaterialType.Unlit, false).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.Unlit, true).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.Unlit, true).Keywords = new string[] { "_MODEL_" };

			GetMaterialCollection(Plugin.RendererMaterialType.BackDistortion, false).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.BackDistortion, false).Keywords = new string[] { "ENABLE_DISTORTION" };
			GetMaterialCollection(Plugin.RendererMaterialType.BackDistortion, true).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.BackDistortion, true).Keywords = new string[] { "_MODEL_", "ENABLE_DISTORTION" };

			GetMaterialCollection(Plugin.RendererMaterialType.Lit, false).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.Lit, false).Keywords = new string[] { "_MODEL_" };
			GetMaterialCollection(Plugin.RendererMaterialType.Lit, true).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.Lit, true).Keywords = new string[] { "_MODEL_", "ENABLE_LIGHTING" };

			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedUnlit, false).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedUnlit, false).Keywords = new string[] { "_ADVANCED_" };
			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedUnlit, true).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedUnlit, true).Keywords = new string[] { "_MODEL_", "_ADVANCED_" };

			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedBackDistortion, false).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedBackDistortion, false).Keywords = new string[] { "ENABLE_DISTORTION", "_ADVANCED_" };
			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedBackDistortion, true).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedBackDistortion, true).Keywords = new string[] { "_MODEL_", "ENABLE_DISTORTION", "_ADVANCED_" };

			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedLit, false).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedLit, false).Keywords = new string[] { "ENABLE_LIGHTING", "_ADVANCED_" };
			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedLit, true).Shader = EffekseerDependentAssets.Instance.fixedShader;
			GetMaterialCollection(Plugin.RendererMaterialType.AdvancedLit, true).Keywords = new string[] { "_MODEL_", "ENABLE_LIGHTING", "_ADVANCED_" };
		}

		// RenderPath per Camera
		private Dictionary<Camera, RenderPath> renderPaths = new Dictionary<Camera, RenderPath>();

		public int layer { get; set; }

#if UNITY_EDITOR
		public bool disableCullingMask { get; set; } = false;
#endif

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
			if (!EffekseerSettings.Instance.renderAsPostProcessingStack)
			{
				Render(camera, null, null, standardBlitter);
			}
		}

		public void Render(Camera camera, RenderTargetProperty renderTargetProperty, CommandBuffer targetCommandBuffer, IEffekseerBlitter blitter)
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

#if UNITY_EDITOR
			if (disableCullingMask)
			{
				mask = camera.cullingMask;
			}
#endif

			// don't need to update because doesn't exists and need not to render
			if ((camera.cullingMask & mask) == 0 && !renderPaths.ContainsKey(camera))
			{
				if (renderPaths.ContainsKey(camera))
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
				path.Init(EffekseerRendererUtils.IsDistortionEnabled, EffekseerRendererUtils.IsDepthEnabled, renderTargetProperty);
				renderPaths.Add(camera, path);
				nextRenderID = (nextRenderID + 1) % EffekseerRendererUtils.RenderIDCount;
			}

			if (!path.IsValid(renderTargetProperty))
			{
				path.Dispose();
				path.Init(EffekseerRendererUtils.IsDistortionEnabled, EffekseerRendererUtils.IsDepthEnabled, renderTargetProperty);
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

			if (path.isCommandBufferFromExternal)
			{
				path.AssignExternalCommandBuffer(targetCommandBuffer);
			}

			// not assigned
			if (path.commandBuffer == null)
			{
				return;
			}

			// assign a dinsotrion texture
			if (path.renderTexture != null)
			{
				Plugin.EffekseerSetExternalTexture(path.renderId, ExternalTextureType.Background, path.renderTexture.ptr);
			}
			else
			{
				Plugin.EffekseerSetExternalTexture(path.renderId, ExternalTextureType.Background, IntPtr.Zero);
			}

			if (path.depthTexture != null)
			{
				Plugin.EffekseerSetExternalTexture(path.renderId, ExternalTextureType.Depth, path.depthTexture.ptr);
			}
			else
			{
				Plugin.EffekseerSetExternalTexture(path.renderId, ExternalTextureType.Depth, IntPtr.Zero);
			}

			// update view matrixes
			Plugin.EffekseerSetProjectionMatrix(path.renderId, Utility.Matrix2Array(
				GL.GetGPUProjectionMatrix(camera.projectionMatrix, false)));
			Plugin.EffekseerSetCameraMatrix(path.renderId, Utility.Matrix2Array(
				camera.worldToCameraMatrix));

			// Reset command buffer
			path.ResetBuffers();

			// copy back
			if (EffekseerRendererUtils.IsDistortionEnabled)
			{
				if (renderTargetProperty != null)
				{
					renderTargetProperty.ApplyToCommandBuffer(path.commandBuffer, path.renderTexture, blitter);

					if (renderTargetProperty.Viewport.width > 0)
					{
						path.commandBuffer.SetViewport(renderTargetProperty.Viewport);
					}
				}
				else
				{
					// TODO : Fix
					bool xrRendering = false;

					blitter.Blit(path.commandBuffer, BuiltinRenderTextureType.CameraTarget, path.renderTexture.renderTexture, xrRendering);
					path.commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown, -1);
				}
			}

			if (path.depthTexture != null)
			{
				if (renderTargetProperty != null)
				{
					renderTargetProperty.ApplyToCommandBuffer(path.commandBuffer, path.depthTexture);

					if (renderTargetProperty.Viewport.width > 0)
					{
						path.commandBuffer.SetViewport(renderTargetProperty.Viewport);
					}
				}
				else
				{
					// TODO : Fix
					bool xrRendering = false;

					blitter.Blit(path.commandBuffer, BuiltinRenderTextureType.Depth, path.depthTexture.renderTexture, xrRendering);
					path.commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown, -1);
				}
			}

			// generate render events on this thread
			Plugin.EffekseerRenderBack(path.renderId);

			// if memory is lacked, reallocate memory
			int maxmumSize = 0;

			for (int i = 0; i < Plugin.GetUnityStrideBufferCount(); i++)
			{
				var buf = Plugin.GetUnityStrideBufferParameter(i);
				maxmumSize = Math.Max(maxmumSize, buf.Size);
			}

			while (Plugin.GetUnityRenderParameterCount() > 0 && maxmumSize > path.computeBufferBack.GetCPUData().Length)
			{
				path.ReallocateComputeBuffer(maxmumSize);
			}

			RenderInternal(path.commandBuffer, path.computeBufferBack, path.materiaProps, path.modelBuffers, path.customDataBuffers, path.renderTexture, path.depthTexture);

			// Distortion
			if (EffekseerRendererUtils.IsDistortionEnabled &&
				(path.renderTexture != null || renderTargetProperty != null))
			{
				// Add a blit command that copy to the distortion texture
				if (renderTargetProperty != null && renderTargetProperty.colorBufferID.HasValue)
				{
					blitter.Blit(path.commandBuffer, renderTargetProperty.colorBufferID.Value, path.renderTexture.renderTexture, renderTargetProperty.xrRendering);
					path.commandBuffer.SetRenderTarget(renderTargetProperty.colorBufferID.Value, 0, CubemapFace.Unknown, -1);

					if (renderTargetProperty.Viewport.width > 0)
					{
						path.commandBuffer.SetViewport(renderTargetProperty.Viewport);
					}
				}
				else if (renderTargetProperty != null)
				{
					renderTargetProperty.ApplyToCommandBuffer(path.commandBuffer, path.renderTexture, blitter);

					if (renderTargetProperty.Viewport.width > 0)
					{
						path.commandBuffer.SetViewport(renderTargetProperty.Viewport);
					}
				}
				else
				{
					// TODO : Fix
					bool xrRendering = false;

					blitter.Blit(path.commandBuffer, BuiltinRenderTextureType.CameraTarget, path.renderTexture.renderTexture, xrRendering);
					path.commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown, -1);
				}
			}

			Plugin.EffekseerRenderFront(path.renderId);

			maxmumSize = 0;

			for (int i = 0; i < Plugin.GetUnityStrideBufferCount(); i++)
			{
				var buf = Plugin.GetUnityStrideBufferParameter(i);
				maxmumSize = Math.Max(maxmumSize, buf.Size);
			}

			// if memory is lacked, reallocate memory
			while (Plugin.GetUnityRenderParameterCount() > 0 && maxmumSize > path.computeBufferFront.GetCPUData().Length)
			{
				path.ReallocateComputeBuffer(maxmumSize);
			}

			RenderInternal(path.commandBuffer, path.computeBufferFront, path.materiaProps, path.modelBuffers, path.customDataBuffers, path.renderTexture, path.depthTexture);
		}

		Texture GetCachedTexture(IntPtr key, BackgroundRenderTexture background, DepthRenderTexture depth, DummyTextureType type)
		{
			if (background != null && background.ptr == key) return background.renderTexture;
			if (depth != null && depth.ptr == key) return depth.renderTexture;

			return EffekseerSystem.GetCachedTexture(key, type);
		}

		Texture GetDepthTexture(DepthRenderTexture depth)
		{
			if (depth != null) return depth.renderTexture;
			return EffekseerSystem.GetCachedTexture(IntPtr.Zero, DummyTextureType.White);
		}

		unsafe void RenderInternal(CommandBuffer commandBuffer, ComputeBufferCollection computeBuffer, MaterialPropCollection matPropCol, ModelBufferCollection modelBufferCol, CustomDataBufferCollection customDataBufferCol, BackgroundRenderTexture background, DepthRenderTexture depth)
		{
			var renderParameterCount = Plugin.GetUnityRenderParameterCount();
			// var vertexBufferSize = Plugin.GetUnityRenderVertexBufferCount();

			if (renderParameterCount > 0)
			{
				for (int i = 0; i < Plugin.GetUnityStrideBufferCount(); i++)
				{
					var buf = Plugin.GetUnityStrideBufferParameter(i);
					if (buf.Size == 0)
					{
						continue;
					}

					Marshal.Copy(buf.Ptr, computeBuffer.GetCPUData(), 0, buf.Size);
					computeBuffer.CopyCPUToGPU(buf.Stride, 0, buf.Size);
				}

				Plugin.UnityRenderParameter parameter = new Plugin.UnityRenderParameter();

				var infoBuffer = Plugin.GetUnityRenderInfoBuffer();

				for (int i = 0; i < renderParameterCount; i++)
				{
					Plugin.GetUnityRenderParameter(ref parameter, i);

					if (parameter.RenderMode == 1)
					{
						RenderModdel(parameter, infoBuffer, commandBuffer, matPropCol, modelBufferCol, customDataBufferCol, background, depth);
					}
					else
					{
						RenderSprite(parameter, infoBuffer, commandBuffer, computeBuffer, matPropCol, background, depth);
					}
				}
			}
		}

		unsafe void RenderSprite(Plugin.UnityRenderParameter parameter, IntPtr infoBuffer, CommandBuffer commandBuffer, ComputeBufferCollection computeBuffer, MaterialPropCollection matPropCol, BackgroundRenderTexture background, DepthRenderTexture depth)
		{
			var prop = matPropCol.GetNext();

			MaterialKey key = new MaterialKey();
			key.Blend = (AlphaBlendType)parameter.Blend;
			key.ZTest = parameter.ZTest > 0;
			key.ZWrite = parameter.ZWrite > 0;
			key.Cull = (int)UnityEngine.Rendering.CullMode.Off;

			prop.SetFloat("buf_offset", parameter.VertexBufferOffset / parameter.VertexBufferStride);

			Debug.Assert(computeBuffer.HasBuffer(parameter.VertexBufferStride));
			var vertexBuffer = computeBuffer.Get(parameter.VertexBufferStride, false);
			if (vertexBuffer == null)
			{
				Debug.LogWarning("Invalid allocation");
				return;
			}
			Debug.Assert(vertexBuffer.IsValid());

			prop.SetBuffer("buf_vertex", vertexBuffer);

			prop.SetVector("mUVInversed", new Vector4(1.0f, -1.0f, 0.0f, 0.0f));
			prop.SetVector("mUVInversedBack", new Vector4(0.0f, 1.0f, 0.0f, 0.0f));

			var isAdvanced = parameter.MaterialType == Plugin.RendererMaterialType.AdvancedBackDistortion ||
				parameter.MaterialType == Plugin.RendererMaterialType.AdvancedLit ||
				parameter.MaterialType == Plugin.RendererMaterialType.AdvancedUnlit;

			if (isAdvanced)
			{
				var bufAd = computeBuffer.Get(sizeof(Effekseer.Plugin.AdvancedVertexParameter), false);
				if (bufAd == null)
				{
					Debug.LogWarning("Invalid allocation");
					return;
				}
				Debug.Assert(bufAd.IsValid());

				prop.SetBuffer("buf_ad", bufAd);
				prop.SetFloat("buf_ad_offset", parameter.AdvancedDataOffset / sizeof(Effekseer.Plugin.AdvancedVertexParameter));

				ApplyAdvancedParameter(parameter, prop);
			}

			ApplyColorSpace(prop);

			prop.SetVector("fEmissiveScaling", new Vector4(parameter.EmissiveScaling, 0.0f, 0.0f, 0.0f));

			ApplyReconstructionParameter(parameter, prop);
			prop.SetVector("softParticleParam", parameter.SoftParticleParam);

			if (parameter.MaterialType == Plugin.RendererMaterialType.Material)
			{
				prop.SetTexture("_depthTex", GetDepthTexture(depth));

				var efkMaterial = EffekseerSystem.GetCachedMaterial(parameter.MaterialPtr);
				if (efkMaterial == null)
				{
					return;
				}

				if (!efkMaterial.IsValid)
				{
					Debug.LogWarning("Please reimport effekseer materials.");
					return;
				}

				Material material = null;

				if (parameter.IsRefraction > 0)
				{
					if (efkMaterial.materialsRefraction == null)
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
				prop.SetColor("lightAmbient", EffekseerSystem.LightAmbientColor);
				prop.SetVector("predefined_uniform", parameter.PredefinedUniform);

				for (int ti = 0; ti < efkMaterial.asset.textures.Length; ti++)
				{
					var texture = GetAndApplyParameterToTexture(parameter, ti, background, depth, DummyTextureType.White);
					if (texture != null)
					{
						prop.SetTexture(efkMaterial.asset.textures[ti].Name, texture);
					}
				}

				AssignUniforms(parameter, infoBuffer, prop, efkMaterial);

				if (parameter.IsRefraction > 0 && background != null)
				{
					prop.SetTexture("_BackTex", GetCachedTexture(parameter.GetTexturePtr(efkMaterial.asset.textures.Length), background, depth, DummyTextureType.White));
				}

				commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
			}
			else
			{
				ApplyTextures(parameter, prop, background, depth);

				var material = GetMaterialCollection(parameter.MaterialType, false).GetMaterial(ref key);
				if (parameter.MaterialType == Plugin.RendererMaterialType.Lit ||
						parameter.MaterialType == Plugin.RendererMaterialType.AdvancedLit)
				{
					prop.SetVector("fLightDirection", EffekseerSystem.LightDirection.normalized);
					prop.SetColor("fLightColor", EffekseerSystem.LightColor);
					prop.SetColor("fLightAmbient", EffekseerSystem.LightAmbientColor);

					commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
				}
				else if (parameter.MaterialType == Plugin.RendererMaterialType.BackDistortion ||
					parameter.MaterialType == Plugin.RendererMaterialType.AdvancedBackDistortion)
				{
					prop.SetVector("g_scale", new Vector4(parameter.DistortionIntensity, 0.0f, 0.0f, 0.0f));

					if (background != null)
					{
						commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
					}
				}
				else
				{
					commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
				}
			}

		}

		private static unsafe void AssignUniforms(Plugin.UnityRenderParameter parameter, IntPtr infoBuffer, MaterialPropertyBlock prop, UnityRendererMaterial efkMaterial)
		{
			int uniformOffset = 0;
			for (int ui = 0; ui < efkMaterial.asset.uniforms.Length; ui++)
			{
				var f = ((float*)(((byte*)infoBuffer.ToPointer()) + parameter.UniformBufferOffset));
				prop.SetVector(efkMaterial.asset.uniforms[ui].Name, new Vector4(f[uniformOffset + 0], f[uniformOffset + 1], f[uniformOffset + 2], f[uniformOffset + 3]));
				uniformOffset += 4;
			}

			for (int gi = 0; gi < efkMaterial.asset.gradients.Length; gi++)
			{
				var gradient = efkMaterial.asset.gradients[gi];

				var f = ((float*)(((byte*)infoBuffer.ToPointer()) + parameter.UniformBufferOffset));

				for (int j = 0; j < 13; j++)
				{
					prop.SetVector(gradient.UniformName + "_" + j.ToString(), new Vector4(f[uniformOffset + 0], f[uniformOffset + 1], f[uniformOffset + 2], f[uniformOffset + 3]));
					uniformOffset += 4;
				}
			}
		}

		unsafe void RenderModdel(Plugin.UnityRenderParameter parameter, IntPtr infoBuffer, CommandBuffer commandBuffer, MaterialPropCollection matPropCol, ModelBufferCollection modelBufferCol1, CustomDataBufferCollection customDataBuffers, BackgroundRenderTexture background, DepthRenderTexture depth)
		{
			// Draw model
			var modelParameters1 = ((Plugin.UnityRenderModelParameter1*)(((byte*)infoBuffer.ToPointer()) + parameter.VertexBufferOffset));
			var modelParameters2 = ((Plugin.UnityRenderModelParameter2*)(((byte*)infoBuffer.ToPointer()) + parameter.AdvancedDataOffset));


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
				ComputeBuffer computeBuf1 = null;
				ComputeBuffer computeBuf2 = null;

				var allocated = modelBufferCol1.Allocate(modelParameters1, modelParameters2, offset, count, ref computeBuf1, ref computeBuf2);

				var isAdvanced = parameter.MaterialType == Plugin.RendererMaterialType.AdvancedBackDistortion ||
					parameter.MaterialType == Plugin.RendererMaterialType.AdvancedLit ||
					parameter.MaterialType == Plugin.RendererMaterialType.AdvancedUnlit;

				if (isAdvanced)
				{
					ApplyAdvancedParameter(parameter, prop);
				}

				ApplyColorSpace(prop);

				prop.SetVector("fEmissiveScaling", new Vector4(parameter.EmissiveScaling, 0.0f, 0.0f, 0.0f));

				ApplyReconstructionParameter(parameter, prop);
				prop.SetVector("softParticleParam", parameter.SoftParticleParam);

				prop.SetBuffer("buf_model_parameter", computeBuf1);

				if (isAdvanced)
				{
					prop.SetBuffer("buf_model_parameter2", computeBuf2);
				}

				prop.SetBuffer("buf_vertex", model.VertexBuffer);
				prop.SetBuffer("buf_index", model.IndexBuffer);
				prop.SetBuffer("buf_vertex_offsets", model.VertexOffsets);
				prop.SetBuffer("buf_index_offsets", model.IndexOffsets);

				prop.SetVector("mUVInversed", new Vector4(1.0f, -1.0f, 0.0f, 0.0f));
				prop.SetVector("mUVInversedBack", new Vector4(0.0f, 1.0f, 0.0f, 0.0f));

				if (parameter.MaterialType == Plugin.RendererMaterialType.Material)
				{
					prop.SetTexture("_depthTex", GetDepthTexture(depth));

					var efkMaterial = EffekseerSystem.GetCachedMaterial(parameter.MaterialPtr);
					if (efkMaterial == null)
					{
						offset += allocated;
						count -= allocated;
						continue;
					}

					if (!efkMaterial.IsValid)
					{
						Debug.LogWarning("Please reimport effekseer materials.");
						offset += allocated;
						count -= allocated;
						continue;
					}

					Material material = null;

					if (parameter.IsRefraction > 0)
					{
						if (efkMaterial.materialsRefraction == null)
						{
							return;
						}

						material = efkMaterial.materialsRefraction.GetMaterial(ref key);
					}
					else
					{
						material = efkMaterial.materials.GetMaterial(ref key);
					}

					if (parameter.IsRefraction > 0)
					{
						material = efkMaterial.materialsModelRefraction.GetMaterial(ref key);
					}
					else
					{
						material = efkMaterial.materialsModel.GetMaterial(ref key);
					}

					prop.SetVector("lightDirection", EffekseerSystem.LightDirection.normalized);
					prop.SetColor("lightColor", EffekseerSystem.LightColor);
					prop.SetColor("lightAmbient", EffekseerSystem.LightAmbientColor);
					prop.SetVector("predefined_uniform", parameter.PredefinedUniform);

					for (int ti = 0; ti < efkMaterial.asset.textures.Length; ti++)
					{
						var texture = GetAndApplyParameterToTexture(parameter, ti, background, depth, DummyTextureType.White);
						if (texture != null)
						{
							prop.SetTexture(efkMaterial.asset.textures[ti].Name, texture);
						}
					}

					AssignUniforms(parameter, infoBuffer, prop, efkMaterial);

					// CustomData
					if (efkMaterial.asset.CustomData1Count > 0)
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
						prop.SetTexture("_BackTex", GetCachedTexture(parameter.GetTexturePtr(efkMaterial.asset.textures.Length), background, depth, DummyTextureType.White));
					}

					commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, model.IndexCounts[0], allocated, prop);
				}
				else
				{
					ApplyTextures(parameter, prop, background, depth);

					var material = GetMaterialCollection(parameter.MaterialType, true).GetMaterial(ref key);
					if (parameter.MaterialType == Plugin.RendererMaterialType.Lit ||
						parameter.MaterialType == Plugin.RendererMaterialType.AdvancedLit)
					{
						prop.SetVector("fLightDirection", EffekseerSystem.LightDirection.normalized);
						prop.SetColor("fLightColor", EffekseerSystem.LightColor);
						prop.SetColor("fLightAmbient", EffekseerSystem.LightAmbientColor);
						commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, model.IndexCounts[0], allocated, prop);
					}
					else if (parameter.MaterialType == Plugin.RendererMaterialType.BackDistortion ||
						parameter.MaterialType == Plugin.RendererMaterialType.AdvancedBackDistortion)
					{
						prop.SetVector("g_scale", new Vector4(parameter.DistortionIntensity, 0.0f, 0.0f, 0.0f));
						if (background != null)
						{
							commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, model.IndexCounts[0], allocated, prop);
						}
					}
					else
					{
						commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, model.IndexCounts[0], allocated, prop);
					}
				}



				offset += allocated;
				count -= allocated;
			}
		}

		unsafe Texture GetAndApplyParameterToTexture(in Plugin.UnityRenderParameter parameter, int index, BackgroundRenderTexture background, DepthRenderTexture depth, DummyTextureType dummyTextureType)
		{
			var texture = GetCachedTexture(parameter.GetTexturePtr(index), background, depth, dummyTextureType);
			if (texture == null)
			{
				return null;
			}

			if (parameter.TextureWrapTypes[index] == 0)
			{
				texture.wrapMode = TextureWrapMode.Repeat;
			}
			else
			{
				texture.wrapMode = TextureWrapMode.Clamp;
			}

			if (parameter.TextureFilterTypes[index] == 0)
			{
				texture.filterMode = FilterMode.Point;
			}
			else
			{
				texture.filterMode = FilterMode.Bilinear;
			}

			return texture;
		}

		unsafe void ApplyTextures(in Plugin.UnityRenderParameter parameter, MaterialPropertyBlock prop, BackgroundRenderTexture background, DepthRenderTexture depth)
		{
			int textureOffset = 0;

			{
				var colorTexture = GetAndApplyParameterToTexture(parameter, textureOffset, background, depth, DummyTextureType.White);
				prop.SetTexture("_colorTex", colorTexture);
				textureOffset += 1;
			}

			if (parameter.MaterialType == Plugin.RendererMaterialType.BackDistortion ||
				parameter.MaterialType == Plugin.RendererMaterialType.AdvancedBackDistortion)
			{
				if (background != null)
				{
					prop.SetTexture("_backTex", GetCachedTexture(parameter.TexturePtrs1, background, depth, DummyTextureType.White));
				}
				textureOffset += 1;
			}

			if (parameter.MaterialType == Plugin.RendererMaterialType.Lit ||
				parameter.MaterialType == Plugin.RendererMaterialType.AdvancedLit)
			{
				var normalTexture = GetAndApplyParameterToTexture(parameter, textureOffset, background, depth, DummyTextureType.Normal);


				prop.SetTexture("_normalTex", normalTexture);
				textureOffset += 1;
			}

			if (parameter.MaterialType == Plugin.RendererMaterialType.AdvancedUnlit || parameter.MaterialType == Plugin.RendererMaterialType.AdvancedLit || parameter.MaterialType == Plugin.RendererMaterialType.AdvancedBackDistortion)
			{
				var alphaTex = GetAndApplyParameterToTexture(parameter, textureOffset, background, depth, DummyTextureType.White);
				prop.SetTexture("_alphaTex", alphaTex);
				textureOffset += 1;

				var uvDistortionTex = GetAndApplyParameterToTexture(parameter, textureOffset, background, depth, DummyTextureType.Normal);
				prop.SetTexture("_uvDistortionTex", uvDistortionTex);
				textureOffset += 1;

				var blendTex = GetAndApplyParameterToTexture(parameter, textureOffset, background, depth, DummyTextureType.White);
				prop.SetTexture("_blendTex", blendTex);
				textureOffset += 1;

				var blendAlphaTex = GetAndApplyParameterToTexture(parameter, textureOffset, background, depth, DummyTextureType.White);
				prop.SetTexture("_blendAlphaTex", blendAlphaTex);
				textureOffset += 1;

				var blendUVDistortionTex = GetAndApplyParameterToTexture(parameter, textureOffset, background, depth, DummyTextureType.Normal);
				prop.SetTexture("_blendUVDistortionTex", blendUVDistortionTex);
				textureOffset += 1;
			}

			{
				if (depth != null)
				{
					prop.SetTexture("_depthTex", GetAndApplyParameterToTexture(parameter, textureOffset, background, depth, DummyTextureType.White));
				}
			}
		}

		void ApplyAdvancedParameter(in Plugin.UnityRenderParameter parameter, MaterialPropertyBlock prop)
		{
			prop.SetVector("flipbookParameter1", new Vector4(
				parameter.FlipbookParams.Enable,
				parameter.FlipbookParams.LoopType,
				parameter.FlipbookParams.DivideX,
				parameter.FlipbookParams.DivideY));

			prop.SetVector("flipbookParameter2", new Vector4(
				parameter.FlipbookParams.OneSizeX,
				parameter.FlipbookParams.OneSizeY,
				parameter.FlipbookParams.OffsetX,
				parameter.FlipbookParams.OffsetY));

			prop.SetVector("fUVDistortionParameter", new Vector4(parameter.UVDistortionIntensity, parameter.BlendUVDistortionIntensity, 1.0f, 0.0f));
			prop.SetVector("fBlendTextureParameter", new Vector4(parameter.TextureBlendType, 0.0f, 0.0f, 0.0f));
			prop.SetVector("fFalloffParameter", new Vector4(parameter.EnableFalloff, parameter.FalloffParam.ColorBlendType, parameter.FalloffParam.Pow, 0.0f));
			prop.SetVector("fFalloffBeginColor", parameter.FalloffParam.BeginColor);
			prop.SetVector("fFalloffEndColor", parameter.FalloffParam.EndColor);
			prop.SetVector("fEdgeColor", parameter.EdgeParams.Color);
			prop.SetVector("fEdgeParameter", new Vector4(parameter.EdgeParams.Threshold, parameter.EdgeParams.ColorScaling, 0.0f, 0.0f));
		}

		void ApplyColorSpace(MaterialPropertyBlock prop)
		{
			prop.SetFloat("convertColorSpace", EffekseerSettings.Instance.MaintainGammaColorInLinearSpace ? 1.0f : 0.0f);
		}

		void ApplyReconstructionParameter(in Plugin.UnityRenderParameter parameter, MaterialPropertyBlock prop)
		{
			prop.SetVector("reconstructionParam1", parameter.ReconstrcutionParam1);
			prop.SetVector("reconstructionParam2", parameter.ReconstrcutionParam2);
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