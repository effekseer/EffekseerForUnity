using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Effekseer.Internal
{
	internal sealed class EffekseerCommandBuffer : IEffekseerCommandBuffer
	{
		readonly CommandBuffer _commandBuffer;

		public EffekseerCommandBuffer(CommandBuffer commandBuffer)
		{
			_commandBuffer = commandBuffer;
		}

		public void SetViewport(Rect viewport)
		{
			_commandBuffer?.SetViewport(viewport);
		}

		public void SetGlobalBuffer(string name, ComputeBuffer buffer)
		{
			_commandBuffer?.SetGlobalBuffer(name, buffer);
		}

		public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount, MaterialPropertyBlock properties)
		{
			_commandBuffer?.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
		}

		public void IssuePluginEvent(IntPtr callback, int eventId)
		{
			_commandBuffer?.IssuePluginEvent(callback, eventId);
		}
	}
}
