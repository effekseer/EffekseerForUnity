using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
#if UNITY_6000_0_OR_NEWER
[RequireComponent(typeof(HDAdditionalCameraData))]
#endif
public class HDRPDynamicResolutionController : MonoBehaviour
{
	const float DefaultScreenPercentage = 100.0f;

	[SerializeField]
	[Range(5.0f, 100.0f)]
	float screenPercentage = 70.0f;

	[SerializeField]
	bool overrideUpscaleFilter = false;

	[SerializeField]
	int upscaleFilter = 0;

#if UNITY_6000_0_OR_NEWER
	static HDRPDynamicResolutionController activeInstance;

	void OnEnable()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		activeInstance = this;
		ApplyCameraSettings();
		ApplyDynamicResolution();
	}

	void Update()
	{
		if (!Application.isPlaying || activeInstance == this)
		{
			return;
		}

		activeInstance = this;
		ApplyCameraSettings();
		ApplyDynamicResolution();
	}

	void OnDisable()
	{
		if (!Application.isPlaying || activeInstance != this)
		{
			return;
		}

		activeInstance = null;
		DynamicResolutionHandler.SetDynamicResScaler(GetDefaultScreenPercentage, DynamicResScalePolicyType.ReturnsPercentage);
	}

	void OnValidate()
	{
		screenPercentage = Mathf.Clamp(screenPercentage, 5.0f, 100.0f);

		if (Application.isPlaying && activeInstance == this)
		{
			ApplyCameraSettings();
			ApplyDynamicResolution();
		}
	}

	void ApplyCameraSettings()
	{
		var targetCamera = GetComponent<Camera>();
		if (!targetCamera.allowDynamicResolution)
		{
			targetCamera.allowDynamicResolution = true;
		}

		var additionalCameraData = GetComponent<HDAdditionalCameraData>();
		if (!additionalCameraData.allowDynamicResolution)
		{
			additionalCameraData.allowDynamicResolution = true;
		}

		if (overrideUpscaleFilter)
		{
			DynamicResolutionHandler.SetUpscaleFilter(targetCamera, (DynamicResUpscaleFilter)upscaleFilter);
		}
	}

	void ApplyDynamicResolution()
	{
		DynamicResolutionHandler.SetDynamicResScaler(GetActiveScreenPercentage, DynamicResScalePolicyType.ReturnsPercentage);
	}

	static float GetActiveScreenPercentage()
	{
		return activeInstance != null ? activeInstance.screenPercentage : DefaultScreenPercentage;
	}

	static float GetDefaultScreenPercentage()
	{
		return DefaultScreenPercentage;
	}
#else
	void OnValidate()
	{
		screenPercentage = Mathf.Clamp(screenPercentage, 5.0f, 100.0f);
	}
#endif
}
