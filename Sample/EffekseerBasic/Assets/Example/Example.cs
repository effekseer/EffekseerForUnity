using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Example: MonoBehaviour {

	EffekseerEmitter emitterA;
	EffekseerEmitter emitterB;
	Button uiEmitterA;
	Button uiEmitterB;
	Dropdown uiEffectList;
	string currentEffectName;
	float cameraAngle = 0.0f;

	void Start () {
		emitterA = GameObject.Find("EffectEmitterA").GetComponent<EffekseerEmitter>();
		emitterB = GameObject.Find("EffectEmitterB").GetComponent<EffekseerEmitter>();
		uiEffectList = GameObject.Find("uiEffectList").GetComponent<Dropdown>();
		SetEffect(0);
		
		uiEmitterA = GameObject.Find("uiPlayAtEmitterA").GetComponent<Button>();
		uiEmitterB = GameObject.Find("uiPlayAtEmitterB").GetComponent<Button>();

		//StartCoroutine(LoadAssetBundle());
	}

	IEnumerator LoadAssetBundle() {
		string url = "file:///" + Application.streamingAssetsPath + "/effects";
		WWW www = new WWW(url);
		yield return www;
		var assetBundle = www.assetBundle;

		EffekseerSystem.LoadEffect("Laser01", assetBundle);
	}
	
	void Update () {
		const float distance = 20.0f;
		const float height = 15.0f;
		cameraAngle += 30 * Mathf.Deg2Rad * Time.deltaTime;
		Camera.main.transform.position = new Vector3(
			distance * Mathf.Cos(cameraAngle), 
			height, 
			distance * Mathf.Sin(cameraAngle));
		Camera.main.transform.LookAt(Vector3.zero);

		// Update Buttons Caption
		var uiTextA = uiEmitterA.transform.Find("Text").GetComponent<Text>();
		if (emitterA.exists) {
			uiTextA.text = "Stop EmitterA";
		} else {
			uiTextA.text = "Play At EmitterA";
		}

		var uiTextB = uiEmitterB.transform.Find("Text").GetComponent<Text>();
		if (emitterB.exists) {
			uiTextB.text = "Stop EmitterB";
		} else {
			uiTextB.text = "Play At EmitterB";
		}
	}

	public void PlayAtZero() {
		var effect = EffekseerSystem.PlayEffect(currentEffectName, Vector3.zero);

		effect.SetTargetLocation(new Vector3(0, 10, 0));
	}
	public void PlayAtRandom() {
		Vector3 position = new Vector3(
			Random.Range(-10.0f, 10.0f),
			Random.Range(  0.0f,  3.0f),
			Random.Range(-10.0f, 10.0f));
		Quaternion rotation = Quaternion.AngleAxis(
			Random.Range(-180.0f, 180.0f),
			Vector3.up);

		var effectHandle = EffekseerSystem.PlayEffect(currentEffectName, position);
		effectHandle.SetRotation(rotation);
	}
	public void PlayAtEmitterA() {
		if (emitterA.exists) {
			emitterA.StopRoot();
		} else {
			emitterA.Play(currentEffectName);
		}
	}
	public void PlayAtEmitterB() {
		if (emitterB.exists) {
			emitterB.StopRoot();
		} else {
			emitterB.Play(currentEffectName);
		}
	}
	public void AllStop() {
		EffekseerSystem.StopAllEffects();
	}
	public void OnValueChanged() {
		SetEffect(uiEffectList.value);
	}
	public void SetEffect(int index) {
		var option = uiEffectList.options[index];
		currentEffectName = option.text;
	}
}
