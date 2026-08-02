using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class URPDynamicResolutionController : MonoBehaviour
{
	const float DefaultScreenPercentage = 100.0f;

	[SerializeField]
	[Range(5.0f, 100.0f)]
	float screenPercentage = 70.0f;

#if UNITY_6000_0_OR_NEWER
	static URPDynamicResolutionController activeInstance;

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
		ApplyScreenPercentage(DefaultScreenPercentage);
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
	}

	void ApplyDynamicResolution()
	{
		ApplyScreenPercentage(GetActiveScreenPercentage());
	}

	static void ApplyScreenPercentage(float percentage)
	{
		var scale = Mathf.Clamp(percentage, 5.0f, 100.0f) * 0.01f;
		scale /= GetPipelineRenderScale();
		scale = Mathf.Clamp(scale, 0.05f, 1.0f);

		ScalableBufferManager.ResizeBuffers(scale, scale);
	}

	static float GetActiveScreenPercentage()
	{
		return activeInstance != null ? activeInstance.screenPercentage : DefaultScreenPercentage;
	}

	static float GetPipelineRenderScale()
	{
		var pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
		return pipelineAsset != null ? Mathf.Max(pipelineAsset.renderScale, 0.01f) : 1.0f;
	}
#else
	void OnValidate()
	{
		screenPercentage = Mathf.Clamp(screenPercentage, 5.0f, 100.0f);
	}
#endif
}
