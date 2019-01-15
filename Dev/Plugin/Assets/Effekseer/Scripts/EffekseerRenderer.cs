using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Effekseer.Internal
{
	public interface IEffekseerRenderer
	{
		int layer { get; set; }

		void SetVisible(bool visible);

		void CleanUp();

		CommandBuffer GetCameraCommandBuffer(Camera camera);
		
		void OnPreCullEvent(Camera camera);

		void OnPostRender(Camera camera);
	}

	class UnityRendererModel : IDisposable
	{
		public ComputeBuffer VertexBuffer;
		public ComputeBuffer IndexBuffer;
		public ComputeBuffer VertexOffsets;
		public ComputeBuffer IndexOffsets;


		public void Initialize(byte[] buffer)
		{
			// temporally
			/*
			List<int> vertexOffsets = new List<int>();
			List<int> indexOffsets = new List<int>();

			int version = 0;
			int offset = 0;
			version = BitConverter.ToInt32(buffer, offset);
			offset += sizeof(int);

			if (version == 2 || version >= 5)
			{
				float scale = BitConverter.ToSingle(buffer, offset);
				offset += sizeof(float);
			}

			int modelCount = BitConverter.ToInt32(buffer, offset);
			offset += sizeof(int);

			int frameCount = 0;

			if (version >= 5)
			{
				frameCount = BitConverter.ToInt32(buffer, offset);
				frameCount += sizeof(int);
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
				offset += sizeof(InternalVertex) * vertexCount;

				indexOffsets.Add(indexBufferCount);

				int faceCount = BitConverter.ToInt32(buffer, offset);

				offset += sizeof(int);

				indexBufferCount += 3 * faceCount;
				offset += sizeof(int) * (3 * faceCount);
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

				vertex.Clear();

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

					VertexBuffer.SetData(vertex, 0, 0, vertex.Count);
				}

				offset += sizeof(InternalVertex) * vertexCount;

				index.Clear();

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

				IndexBuffer.SetData(index, 0, 0, index.Count);
			}

			VertexOffsets = new ComputeBuffer(vertexOffsets.Count, sizeof(int));
			IndexOffsets = new ComputeBuffer(indexOffsets.Count, sizeof(int));
			VertexOffsets.SetData(vertexOffsets);
			IndexOffsets.SetData(indexOffsets);
			*/
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

	struct InternalVertex
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector3 Binormal;
		public Vector3 Tangent;
		public Vector2 UV;
		public Color32 VColor;
	}

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

		public enum AlphaBlendType : int
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

			public int GetKey()
			{
				return (int)Blend +
					(ZTest ? 1 : 0) << 4 +
					(ZWrite ? 1 : 0) << 5;
			}
		}

		class MaterialCollection
		{
			public Shader Shader;
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
				}
				else if (key.Blend == AlphaBlendType.Blend)
				{
					material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				}
				else if (key.Blend == AlphaBlendType.Add)
				{
					material.SetFloat("_BlendSrc", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetFloat("_BlendDst", (float)UnityEngine.Rendering.BlendMode.One);
				}
				else
				{
					throw new Exception();
				}

				material.SetFloat("_ZTest", key.ZTest ? (float)UnityEngine.Rendering.CompareFunction.LessEqual : (float)UnityEngine.Rendering.CompareFunction.Disabled);
				material.SetFloat("_ZWrite", key.ZWrite ? 1.0f : 0.0f);

				materials.Add(id, material);

				return material;
			}
		}

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

		private class RenderPath : IDisposable
		{
			const int VertexMaxCount = 8192;
			public Camera camera;
			public CommandBuffer commandBuffer;
			public CameraEvent cameraEvent;
			public int renderId;
			public RenderTexture renderTexture;
			public ComputeBuffer computeBufferFront;
			public ComputeBuffer computeBufferBack;
			public byte[] computeBufferTemp;

			public MaterialPropCollection materiaProps = new MaterialPropCollection();

			public RenderPath(Camera camera, CameraEvent cameraEvent, int renderId)
			{
				this.camera = camera;
				this.renderId = renderId;
				this.cameraEvent = cameraEvent;
			}

			public void Init(bool enableDistortion)
			{
				// Create a command buffer that is effekseer renderer
				this.commandBuffer = new CommandBuffer();
				this.commandBuffer.name = "Effekseer Rendering";

				// register the command to a camera
				this.camera.AddCommandBuffer(this.cameraEvent, this.commandBuffer);

				if (enableDistortion)
				{
					RenderTextureFormat format = (this.camera.allowHDR) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
					this.renderTexture = new RenderTexture(this.camera.pixelWidth, this.camera.pixelHeight, 0, format);
					this.renderTexture.Create();
				}

				computeBufferFront = new ComputeBuffer(VertexMaxCount, VertexSize, ComputeBufferType.Default);
				computeBufferBack = new ComputeBuffer(VertexMaxCount, VertexSize, ComputeBufferType.Default);
				computeBufferTemp = new byte[VertexMaxCount * VertexSize];
			}

			public void Dispose()
			{
				if (this.commandBuffer != null)
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
			}

			public bool IsValid()
			{
				if (this.renderTexture != null)
				{
					return this.camera.pixelWidth == this.renderTexture.width &&
						this.camera.pixelHeight == this.renderTexture.height;
				}
				return true;
			}
		};

		MaterialCollection materials = new MaterialCollection();
		MaterialCollection materialsDistortion = new MaterialCollection();

		public EffekseerRendererUnity()
		{
			materials.Shader = EffekseerSettings.Instance.standardShader;
			materialsDistortion.Shader = EffekseerSettings.Instance.standardDistortionShader;
		}

		// RenderPath per Camera
		private Dictionary<Camera, RenderPath> renderPaths = new Dictionary<Camera, RenderPath>();

		public int layer { get; set; }

		public void SetVisible(bool visible)
		{
			if (visible)
			{
				Camera.onPreCull += OnPreCullEvent;
				Camera.onPostRender += OnPostRender;
			}
			else
			{
				Camera.onPreCull -= OnPreCullEvent;
				Camera.onPostRender -= OnPostRender;
			}
		}

		public void CleanUp()
		{
			// dispose all render pathes
			foreach (var pair in renderPaths)
			{
				pair.Value.Dispose();
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

		public void OnPreCullEvent(Camera camera)
		{
			var settings = EffekseerSettings.Instance;

#if UNITY_EDITOR
			if (camera.cameraType == CameraType.SceneView)
			{
				// シーンビューのカメラはチェック
				if (settings.drawInSceneView == false)
				{
					return;
				}
			}
#endif
			RenderPath path;

			// カリングマスクをチェック
			if ((camera.cullingMask & (1 << layer)) == 0)
			{
				if (renderPaths.ContainsKey(camera))
				{
					// レンダーパスが存在すればコマンドバッファを解除
					path = renderPaths[camera];
					path.Dispose();
					renderPaths.Remove(camera);
				}
				return;
			}

			if (renderPaths.ContainsKey(camera))
			{
				// レンダーパスが有れば使う
				path = renderPaths[camera];
			}
			else
			{
				// 無ければレンダーパスを作成
				path = new RenderPath(camera, cameraEvent, renderPaths.Count);
				path.Init(settings.enableDistortion);
				renderPaths.Add(camera, path);
			}

			if (!path.IsValid())
			{
				path.Dispose();
				path.Init(settings.enableDistortion);
			}

			// 歪みテクスチャをセット
			if (path.renderTexture)
			{
				Plugin.EffekseerSetBackGroundTexture(path.renderId, path.renderTexture.GetNativeTexturePtr());
			}

			// ステレオレンダリング(VR)用に左右目の行列を設定
			if (camera.stereoEnabled)
			{
				float[] projMatL = Utility.Matrix2Array(GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), false));
				float[] projMatR = Utility.Matrix2Array(GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), false));
				float[] camMatL = Utility.Matrix2Array(camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left));
				float[] camMatR = Utility.Matrix2Array(camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right));
				Plugin.EffekseerSetStereoRenderingMatrix(path.renderId, projMatL, projMatR, camMatL, camMatR);
			}
			else
			{
				// ビュー関連の行列を更新
				Plugin.EffekseerSetProjectionMatrix(path.renderId, Utility.Matrix2Array(
					GL.GetGPUProjectionMatrix(camera.projectionMatrix, false)));
				Plugin.EffekseerSetCameraMatrix(path.renderId, Utility.Matrix2Array(
					camera.worldToCameraMatrix));
			}

			// Reset command buffer
			path.commandBuffer.Clear();
			path.materiaProps.Reset();

			// generate render events on this thread
			Plugin.EffekseerRenderBack(path.renderId);
			RenderInternal(path.commandBuffer, path.computeBufferTemp, path.computeBufferBack, path.materiaProps);

			// Distortion
			if (settings.enableDistortion && path.renderTexture != null)
			{
				// Add a blit command that copy to the distortion texture
				path.commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, path.renderTexture);
				path.commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

			}

			Plugin.EffekseerRenderFront(path.renderId);
			RenderInternal(path.commandBuffer, path.computeBufferTemp, path.computeBufferFront, path.materiaProps);
		}

		void RenderInternal(CommandBuffer commandBuffer, byte[] computeBufferTemp, ComputeBuffer computeBuffer, MaterialPropCollection matPropCol)
		{
			var renderParameterCount = Plugin.GetUnityRenderParameterCount();
			var vertexBufferSize = Plugin.GetUnityRenderVertexBufferCount();

			if (renderParameterCount > 0)
			{
				Plugin.UnityRenderParameter parameter = new Plugin.UnityRenderParameter();

				var vertexBuffer = Plugin.GetUnityRenderVertexBuffer();
				var vertexBufferCount = Plugin.GetUnityRenderVertexBufferCount();

				System.Runtime.InteropServices.Marshal.Copy(vertexBuffer, computeBufferTemp, 0, vertexBufferCount);
				computeBuffer.SetData(computeBufferTemp, 0, 0, vertexBufferCount);

				for (int i = 0; i < renderParameterCount; i++)
				{
					var prop = matPropCol.GetNext();
					Plugin.GetUnityRenderParameter(ref parameter, i);

					if (parameter.IsDistortingMode > 0)
					{
						MaterialKey key = new MaterialKey();
						key.Blend = (AlphaBlendType)parameter.Blend;
						key.ZTest = parameter.ZTest > 0;
						key.ZWrite = parameter.ZWrite > 0;
						var material = materialsDistortion.GetMaterial(ref key);

						prop.SetFloat("buf_offset", parameter.VertexBufferOffset / VertexDistortionSize);
						prop.SetBuffer("buf_vertex", computeBuffer);
						prop.SetTexture("_ColorTex", EffekseerSystem.GetCachedTexture(parameter.TexturePtrs0));

						commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
					}
					else
					{
						MaterialKey key = new MaterialKey();
						key.Blend = (AlphaBlendType)parameter.Blend;
						key.ZTest = parameter.ZTest > 0;
						key.ZWrite = parameter.ZWrite > 0;
						var material = materials.GetMaterial(ref key);

						prop.SetFloat("buf_offset", parameter.VertexBufferOffset / VertexSize);
						prop.SetBuffer("buf_vertex", computeBuffer);
						prop.SetTexture("_ColorTex", EffekseerSystem.GetCachedTexture(parameter.TexturePtrs0));

						commandBuffer.DrawProcedural(new Matrix4x4(), material, 0, MeshTopology.Triangles, parameter.ElementCount * 2 * 3, 1, prop);
					}
				}
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

	internal class EffekseerRendererNative : IEffekseerRenderer
	{
		const CameraEvent cameraEvent = CameraEvent.AfterForwardAlpha;

		private class RenderPath : IDisposable
		{
			public Camera camera;
			public CommandBuffer commandBuffer;
			public CameraEvent cameraEvent;
			public int renderId;
			public RenderTexture renderTexture;

			public RenderPath(Camera camera, CameraEvent cameraEvent, int renderId)
			{
				this.camera = camera;
				this.renderId = renderId;
				this.cameraEvent = cameraEvent;
			}

			public void Init(bool enableDistortion)
			{
				// Create a command buffer that is effekseer renderer
				this.commandBuffer = new CommandBuffer();
				this.commandBuffer.name = "Effekseer Rendering";

				// add a command to render effects.
				this.commandBuffer.IssuePluginEvent(Plugin.EffekseerGetRenderBackFunc(), this.renderId);

#if UNITY_5_6_OR_NEWER
				if (enableDistortion)
				{
					RenderTextureFormat format = (this.camera.allowHDR) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#else
				if (enableDistortion && camera.cameraType == CameraType.Game) {
					RenderTextureFormat format = (camera.hdr) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#endif

					// Create a distortion texture
					this.renderTexture = new RenderTexture(this.camera.pixelWidth, this.camera.pixelHeight, 0, format);
					this.renderTexture.Create();
					// Add a blit command that copy to the distortion texture
					this.commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, this.renderTexture);
					this.commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

				}

				this.commandBuffer.IssuePluginEvent(Plugin.EffekseerGetRenderFrontFunc(), this.renderId);

				// register the command to a camera
				this.camera.AddCommandBuffer(this.cameraEvent, this.commandBuffer);
			}

			public void Dispose()
			{
				if (this.commandBuffer != null)
				{
					if (this.camera != null)
					{
						this.camera.RemoveCommandBuffer(this.cameraEvent, this.commandBuffer);
					}
					this.commandBuffer.Dispose();
					this.commandBuffer = null;
				}
			}

			public bool IsValid()
			{
				if (this.renderTexture != null)
				{
					return this.camera.pixelWidth == this.renderTexture.width &&
						this.camera.pixelHeight == this.renderTexture.height;
				}
				return true;
			}
		};

		// RenderPath per Camera
		private Dictionary<Camera, RenderPath> renderPaths = new Dictionary<Camera, RenderPath>();

		public int layer { get; set; }

		public void SetVisible(bool visible)
		{
			if (visible)
			{
				Camera.onPreCull += OnPreCullEvent;
				Camera.onPostRender += OnPostRender;
			}
			else
			{
				Camera.onPreCull -= OnPreCullEvent;
				Camera.onPostRender -= OnPostRender;
			}
		}

		public void CleanUp()
		{
			// レンダーパスの全破棄
			foreach (var pair in renderPaths)
			{
				pair.Value.Dispose();
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

		public void OnPreCullEvent(Camera camera)
		{
			var settings = EffekseerSettings.Instance;

#if UNITY_EDITOR
			if (camera.cameraType == CameraType.SceneView)
			{
				// シーンビューのカメラはチェック
				if (settings.drawInSceneView == false)
				{
					return;
				}
			}
#endif
			RenderPath path;

			// カリングマスクをチェック
			if ((camera.cullingMask & (1 << layer)) == 0)
			{
				if (renderPaths.ContainsKey(camera))
				{
					// レンダーパスが存在すればコマンドバッファを解除
					path = renderPaths[camera];
					path.Dispose();
					renderPaths.Remove(camera);
				}
				return;
			}

			if (renderPaths.ContainsKey(camera))
			{
				// レンダーパスが有れば使う
				path = renderPaths[camera];
			}
			else
			{
				// 無ければレンダーパスを作成
				path = new RenderPath(camera, cameraEvent, renderPaths.Count);
				path.Init(settings.enableDistortion);
				renderPaths.Add(camera, path);
			}

			if (!path.IsValid())
			{
				path.Dispose();
				path.Init(settings.enableDistortion);
			}

			// 歪みテクスチャをセット
			if (path.renderTexture)
			{
				Plugin.EffekseerSetBackGroundTexture(path.renderId, path.renderTexture.GetNativeTexturePtr());
			}

#if UNITY_5_4_OR_NEWER
			// ステレオレンダリング(VR)用に左右目の行列を設定
			if (camera.stereoEnabled)
			{
				float[] projMatL = Utility.Matrix2Array(GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), false));
				float[] projMatR = Utility.Matrix2Array(GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), false));
				float[] camMatL = Utility.Matrix2Array(camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left));
				float[] camMatR = Utility.Matrix2Array(camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right));
				Plugin.EffekseerSetStereoRenderingMatrix(path.renderId, projMatL, projMatR, camMatL, camMatR);
			}
			else
#endif
			{
				// ビュー関連の行列を更新
				Plugin.EffekseerSetProjectionMatrix(path.renderId, Utility.Matrix2Array(
					GL.GetGPUProjectionMatrix(camera.projectionMatrix, false)));
				Plugin.EffekseerSetCameraMatrix(path.renderId, Utility.Matrix2Array(
					camera.worldToCameraMatrix));
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