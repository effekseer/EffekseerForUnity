using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_2017_1_OR_NEWER
using UnityEngine.Playables;
#endif

#if UNITY_2017_1_OR_NEWER
[System.Serializable]
public class EffekseerPlayableAsset : PlayableAsset
{
	public string effectName;
	public ExposedReference<GameObject> emitterObject;
	public ExposedReference<GameObject> targetObject;
	
	// Factory method that generates a playable based on this asset
	public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
	{
		var behaviour = new EffekseerPlayableBehaviour();
		behaviour.effectName = this.effectName;
		behaviour.emitterObject = this.emitterObject.Resolve(graph.GetResolver());
		behaviour.targetObject = this.targetObject.Resolve(graph.GetResolver());
		return ScriptPlayable<EffekseerPlayableBehaviour>.Create(graph, behaviour);
	}
}
#endif