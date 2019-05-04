using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EfkChaingingSceneScript : MonoBehaviour {

	float time = 0;

	public float changingTime = 5;

	public string target = "EfkChangingScene1";

	bool isRemoveUnusedCalled = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		time += UnityEngine.Time.deltaTime;

		if(time > changingTime / 2)
		{
			if(!isRemoveUnusedCalled)
			{
				Resources.UnloadUnusedAssets();
				isRemoveUnusedCalled = true;
			}
		}

		if(time > changingTime)
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(target);
		}
	}
}
