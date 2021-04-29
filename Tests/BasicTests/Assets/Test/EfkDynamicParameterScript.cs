using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EfkDynamicParameterScript : MonoBehaviour {

	public GameObject TargetObject = null;

	Effekseer.EffekseerEmitter emitter = null;

	// Use this for initialization
	void Start () {
		emitter = GetComponent<Effekseer.EffekseerEmitter>();	
	}
	
	// Update is called once per frame
	void Update () {
		if(emitter != null)
		{
			var pos = TargetObject.transform.position;
			emitter.SetDynamicInputWithWorldPosition(ref pos);
		}
	}
}
