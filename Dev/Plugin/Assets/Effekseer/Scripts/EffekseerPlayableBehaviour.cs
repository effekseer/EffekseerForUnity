using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2017_1_OR_NEWER
using UnityEngine.Playables;
#endif

namespace Effekseer
{
#if UNITY_2017_1_OR_NEWER
	public class EffekseerPlayableBehaviour : PlayableBehaviour
	{
		public EffekseerEffectAsset effectAsset;
		public GameObject emitterObject;
		public GameObject targetObject;

		private EffekseerHandle handle;
		private double lastFrameTime;

		public override void OnPlayableCreate(Playable playable)
		{
		}

		public override void OnPlayableDestroy(Playable playable)
		{
		}

		// Called when the owning graph starts playing
		public override void OnGraphStart(Playable playable)
		{
		}

		// Called when the owning graph stops playing
		public override void OnGraphStop(Playable playable)
		{
		}

		// Called when the state of the playable is set to Play
		public override void OnBehaviourPlay(Playable playable, FrameData info)
		{
			PlayEffect();
		}

		// Called when the state of the playable is set to Paused
		public override void OnBehaviourPause(Playable playable, FrameData info)
		{
			StopEffect();
		}

		// Called each frame while the state is set to Play
		public override void PrepareFrame(Playable playable, FrameData info)
		{
			double time = playable.GetTime();
			double elapsedTime = time - this.lastFrameTime;
			this.lastFrameTime = time;

			if (!Application.isPlaying)
			{
				if (elapsedTime < 0.0)
				{
					StopEffect();
					PlayEffect();
					elapsedTime = time;
				}
			}

			if (!handle.enabled)
			{
				return;
			}

			if (emitterObject)
			{
				handle.SetLocation(emitterObject.transform.position);
				handle.SetRotation(emitterObject.transform.rotation);
				handle.SetScale(emitterObject.transform.localScale);
			}
			if (targetObject)
			{
				handle.SetTargetLocation(targetObject.transform.position);
			}

			if (!Application.isPlaying)
			{
				double frames = elapsedTime * 60.0f;
				for (int i = 0; i < frames; i++)
				{
					handle.UpdateHandle(1.0f);
				}
			}
		}

		private void PlayEffect()
		{
			if (effectAsset != null)
			{
				handle = EffekseerSystem.PlayEffect(effectAsset,
					(emitterObject) ? emitterObject.transform.position : Vector3.zero);
			}
		}

		private void StopEffect()
		{
			if (!handle.enabled)
			{
				return;
			}
			handle.Stop();
			handle.UpdateHandle(1.0f);
			Effekseer.Plugin.EffekseerResetTime();
		}

		private void SetTime()
		{
		}
	}
#endif
}
