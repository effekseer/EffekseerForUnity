using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2017_1_OR_NEWER
using UnityEngine.Playables;
#endif

#if UNITY_2017_1_OR_NEWER
public class EffekseerPlayableBehaviour : PlayableBehaviour
{
	public string effectName;
	public GameObject emitterObject;
	public GameObject targetObject;

	private EffekseerHandle effectHandle;
	private double lastFrameTime;

	public override void OnPlayableCreate(Playable playable) {
		if (!Application.isPlaying) {
			EffekseerSystem.Instance.Init();
		}
	}
	
	public override void OnPlayableDestroy(Playable playable) {
		if (!Application.isPlaying) {
			EffekseerSystem.Instance.Term();
		}
	}
	
	// Called when the owning graph starts playing
	public override void OnGraphStart(Playable playable) {
	}

	// Called when the owning graph stops playing
	public override void OnGraphStop(Playable playable) {
	}

	// Called when the state of the playable is set to Play
	public override void OnBehaviourPlay(Playable playable, FrameData info) {
		PlayEffect();
	}

	// Called when the state of the playable is set to Paused
	public override void OnBehaviourPause(Playable playable, FrameData info) {
		StopEffect();
	}
	
	// Called each frame while the state is set to Play
	public override void PrepareFrame(Playable playable, FrameData info) {
		double time = playable.GetTime();
		double elapsedTime = time - this.lastFrameTime;
		this.lastFrameTime = time;

		if (!Application.isPlaying) {
			if (elapsedTime < 0.0) {
				StopEffect();
				PlayEffect();
				elapsedTime = time;
			}
		}

		if (!effectHandle.enabled) {
			return;
		}

		if (emitterObject) {
			effectHandle.SetLocation(emitterObject.transform.position);
			effectHandle.SetRotation(emitterObject.transform.rotation);
			effectHandle.SetScale(emitterObject.transform.localScale);
		}
		if (targetObject) {
			effectHandle.SetTargetLocation(targetObject.transform.position);
		}

		if (!Application.isPlaying) {
			double frames = elapsedTime * 60.0f;
			for (int i = 0; i < frames; i++) {
				effectHandle.UpdateHandle(1.0f);
			}
		}
	}

	private void PlayEffect() {
		if (!String.IsNullOrEmpty(effectName) && emitterObject) {
			effectHandle = EffekseerSystem.PlayEffect(effectName, 
				emitterObject.transform.position);
		}
	}
	
	private void StopEffect() {
		if (!effectHandle.enabled) {
			return;
		}
		effectHandle.Stop();
		effectHandle.UpdateHandle(1.0f);
	}

	private void SetTime() {
	}
}
#endif