using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EfkChaingingScene1Script : MonoBehaviour {

	float time = 0;

	public float chaingingTime = 5;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		time += UnityEngine.Time.deltaTime;

		if(time > chaingingTime)
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene("EfkChangingScene1");
		}
	}
}
