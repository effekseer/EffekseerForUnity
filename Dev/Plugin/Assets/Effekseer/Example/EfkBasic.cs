using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Effekseer;

public class EfkBasic : MonoBehaviour
{
	public List<EffekseerEffectAsset> effectAssets = new List<EffekseerEffectAsset>();

	EffekseerEmitter emitterA;
	EffekseerEmitter emitterB;
	Dropdown uiEffectList;
	Button uiEffekseerEmitterA;
	Button uiEffekseerEmitterB;
	EffekseerEffectAsset currentEffect = null;
	float cameraAngle = 0.0f;
	
	void Start()
	{
		emitterA = GameObject.Find("EffectEmitterA").GetComponent<EffekseerEmitter>();
		emitterB = GameObject.Find("EffectEmitterB").GetComponent<EffekseerEmitter>();
		uiEffectList = GameObject.Find("uiEffectList").GetComponent<Dropdown>();
		uiEffekseerEmitterA = GameObject.Find("uiPlayAtEmitterA").GetComponent<Button>();
		uiEffekseerEmitterB = GameObject.Find("uiPlayAtEmitterB").GetComponent<Button>();
		
		foreach (var effectAsset in effectAssets) {
			if (effectAsset != null) {
				uiEffectList.options.Add(new Dropdown.OptionData(effectAsset.name));
			}
		}
		
		SetEffect(3);
		uiEffectList.value = 3;
	}

	void OnEnable()
	{
	}

	void Update()
	{
		const float distance = 20.0f;
		const float height = 15.0f;
		cameraAngle += 30 * Mathf.Deg2Rad * Time.deltaTime;
		Camera.main.transform.position = new Vector3(
			distance * Mathf.Cos(cameraAngle), 
			height, 
			distance * Mathf.Sin(cameraAngle));
		Camera.main.transform.LookAt(Vector3.zero);

		// Update Buttons Caption
		if (uiEffekseerEmitterA) {
			var uiTextA = uiEffekseerEmitterA.transform.Find("Text").GetComponent<Text>();
			if (emitterA.exists) {
				uiTextA.text = "Stop EmitterA";
			} else {
				uiTextA.text = "Play At EmitterA";
			}
		}

		if (uiEffekseerEmitterB) {
			var uiTextB = uiEffekseerEmitterB.transform.Find("Text").GetComponent<Text>();
			if (emitterB.exists) {
				uiTextB.text = "Stop EmitterB";
			} else {
				uiTextB.text = "Play At EmitterB";
			}
		}
	}

	public void PlayAtZero()
	{
		EffekseerSystem.PlayEffect(currentEffect, Vector3.zero);
	}

	public void PlayAtRandom()
	{
		Vector3 position = new Vector3(
			Random.Range(-10.0f, 10.0f),
			Random.Range(  0.0f,  3.0f),
			Random.Range(-10.0f, 10.0f));
		Quaternion rotation = Quaternion.AngleAxis(
			Random.Range(-180.0f, 180.0f),
			Vector3.up);

		var handle = EffekseerSystem.PlayEffect(currentEffect, position);
		handle.SetRotation(rotation);
	}

	public void PlayAtEmitterA()
	{
		if (emitterA.exists) {
			emitterA.StopRoot();
		} else {
			emitterA.Play(currentEffect);
		}
	}

	public void PlayAtEmitterB()
	{
		if (emitterB.exists) {
			emitterB.StopRoot();
		} else {
			emitterB.Play(currentEffect);
		}
	}

	public void StopAll()
	{
		EffekseerSystem.StopAllEffects();
	}

	public void OnValueChanged()
	{
		SetEffect(uiEffectList.value);
	}

	public void SetEffect(int index)
	{
		currentEffect = effectAssets[index];
	}
}
