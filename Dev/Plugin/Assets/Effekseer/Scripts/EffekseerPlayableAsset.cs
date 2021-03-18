using System;
using UnityEngine;

#if UNITY_2017_1_OR_NEWER
using UnityEngine.Playables;
#endif

namespace Effekseer
{
#if UNITY_2017_1_OR_NEWER
	[Serializable]
	public class EffekseerPlayableAsset : PlayableAsset
	{
		public EffekseerEffectAsset effectAsset;
		public ExposedReference<GameObject> emitterObject;
		public ExposedReference<GameObject> targetObject;

		// Factory method that generates a playable based on this asset
		public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
		{
			var behaviour = new EffekseerPlayableBehaviour();
			behaviour.effectAsset = this.effectAsset;
			behaviour.emitterObject = this.emitterObject.Resolve(graph.GetResolver());
			behaviour.targetObject = this.targetObject.Resolve(graph.GetResolver());
			return ScriptPlayable<EffekseerPlayableBehaviour>.Create(graph, behaviour);
		}
	}
#endif
}
