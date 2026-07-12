#if EFFEKSEER_URP_SUPPORT && UNITY_6000_0_OR_NEWER

using System;
using Effekseer.Internal;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class EffekseerURPRasterCommandBuffer : IEffekseerCommandBuffer
{
	readonly RasterCommandBuffer _commandBuffer;

	public EffekseerURPRasterCommandBuffer(RasterCommandBuffer commandBuffer)
	{
		_commandBuffer = commandBuffer;
	}

	public void SetViewport(Rect viewport)
	{
		_commandBuffer.SetViewport(viewport);
	}

	public void SetGlobalBuffer(string name, ComputeBuffer buffer)
	{
		_commandBuffer.SetGlobalBuffer(name, buffer);
	}

	public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology,
		int vertexCount, int instanceCount, MaterialPropertyBlock properties)
	{
		_commandBuffer.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
	}

	public void IssuePluginEvent(IntPtr callback, int eventId)
	{
		_commandBuffer.IssuePluginEvent(callback, eventId);
	}
}

#endif
