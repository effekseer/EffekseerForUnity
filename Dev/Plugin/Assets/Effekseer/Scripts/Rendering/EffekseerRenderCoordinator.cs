using UnityEngine;
using UnityEngine.Rendering;

namespace Effekseer.Internal
{
	public static class EffekseerRenderCoordinator
	{
		public static void Render(IEffekseerRenderer renderer, EffekseerRenderFrameInput input)
		{
			var frame = renderer.PrepareFrame(input);
			if (frame == null || !frame.HasVisibleEffects || frame.CommandBuffer == null)
			{
				return;
			}

			var commandBuffer = new EffekseerCommandBuffer(frame.CommandBuffer);
			PreparePhaseTarget(input, frame, true);
			CommitNativeTargetIfRequired(frame, commandBuffer);
			renderer.RecordPhase(frame, EffekseerRenderPhase.Back, commandBuffer);

			PreparePhaseTarget(input, frame, false);
			CommitNativeTargetIfRequired(frame, commandBuffer);
			renderer.RecordPhase(frame, EffekseerRenderPhase.Front, commandBuffer);
			renderer.EndFrame(frame);
		}

		public static void RenderExternal(IEffekseerRenderer renderer, EffekseerRenderFrameInput input, IEffekseerCommandBuffer commandBuffer)
		{
			var frame = renderer.PrepareFrame(input);
			if (frame == null || !frame.HasVisibleEffects)
			{
				return;
			}

			renderer.RecordPhase(frame, EffekseerRenderPhase.Back, commandBuffer);
			renderer.RecordPhase(frame, EffekseerRenderPhase.Front, commandBuffer);
			renderer.EndFrame(frame);
		}

		static void PreparePhaseTarget(EffekseerRenderFrameInput input, EffekseerPreparedFrame frame, bool includeDepth)
		{
			var commandBuffer = frame.CommandBuffer;
			var target = input.RenderTargetProperty;
			var blitter = input.Blitter;

			if (target != null && input.SetDefaultRenderTarget)
			{
				target.SetDefaultRenderTarget(commandBuffer, blitter);
			}

			if (frame.Background != null)
			{
				if (target != null)
				{
					target.ApplyToCommandBuffer(commandBuffer, frame.Background, blitter);
				}
				else
				{
					blitter.Blit(commandBuffer, BuiltinRenderTextureType.CameraTarget, frame.Background.renderTexture, false);
					blitter.SetRenderTarget(commandBuffer, BuiltinRenderTextureType.CameraTarget, false);
				}
			}

			if (includeDepth && frame.Depth != null)
			{
				if (target != null)
				{
					target.ApplyToCommandBuffer(commandBuffer, frame.Depth, blitter);
				}
				else
				{
					blitter.Blit(commandBuffer, BuiltinRenderTextureType.Depth, frame.Depth.renderTexture, false);
					blitter.SetRenderTarget(commandBuffer, BuiltinRenderTextureType.CameraTarget, false);
				}
			}

			if (target != null && target.Viewport.HasValue)
			{
				commandBuffer.SetViewport(target.Viewport.Value);
			}
		}

		static void CommitNativeTargetIfRequired(EffekseerPreparedFrame frame, IEffekseerCommandBuffer commandBuffer)
		{
			if (!frame.IsNativeRenderer || frame.TargetCommitMaterial == null)
			{
				return;
			}

			// XR-WA-NATIVE-TARGET-COMMIT (English): On the affected Editor/PS4 native-renderer path,
			// SetRenderTarget is not committed before the plugin event unless a draw is recorded.
			// This zero-impact fake draw commits the intended target. Remove it only after every
			// supported graphics API applies the target before IssuePluginEvent without this draw.
			// XR-WA-NATIVE-TARGET-COMMIT (日本語): 対象となる Editor/PS4 の Native Renderer 経路では、
			// 描画を記録しないと SetRenderTarget がプラグインイベント前に確定しません。
			// この表示に影響しない Fake Draw で目的のターゲットを確定します。対応対象の全 Graphics API で
			// Fake Draw なしに IssuePluginEvent 前へ反映されることを確認できた場合のみ削除してください。
			commandBuffer.DrawProcedural(Matrix4x4.identity, frame.TargetCommitMaterial, 0, MeshTopology.Triangles, 3, 1, null);
		}
	}
}
