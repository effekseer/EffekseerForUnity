using System;
using UnityEngine;

namespace Effekseer.Internal
{
	public interface IEffekseerCommandBuffer
	{
		void SetViewport(Rect viewport);
		void SetGlobalBuffer(string name, ComputeBuffer buffer);
		void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount, MaterialPropertyBlock properties);
		void IssuePluginEvent(IntPtr callback, int eventId);
	}
}
