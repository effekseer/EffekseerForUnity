using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ExampleGUI : MonoBehaviour {

	EffekseerEmitter emitterA;
	EffekseerEmitter emitterB;
	Dropdown uiEffectList;
	string currentEffectName;

	// Use this for initialization
	void Start () {
		emitterA = GameObject.Find("EffectEmitterA").GetComponent<EffekseerEmitter>();
		emitterB = GameObject.Find("EffectEmitterB").GetComponent<EffekseerEmitter>();
		uiEffectList = GameObject.Find("uiEffectList").GetComponent<Dropdown>();
		SetEffect(0);
	}
	
	// Update is called once per frame
	void Update () {
	}

	public void PlayAtZero() {
		EffekseerSystem.PlayEffect(currentEffectName, Vector3.zero);
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
		emitterB.Play(currentEffectName);
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
