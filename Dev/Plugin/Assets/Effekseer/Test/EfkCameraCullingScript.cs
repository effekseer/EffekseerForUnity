using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EfkCameraCullingScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Effekseer.EffekseerSettings.Instance.layer = 1;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnDestroy()
	{
		Effekseer.EffekseerSettings.Instance.layer = 0;
	}
}
