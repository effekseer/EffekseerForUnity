
#ifndef	__EFFEKSEER_PLUGIN_COMMON_H__
#define __EFFEKSEER_PLUGIN_COMMON_H__

#include <vector>
#include "Effekseer.h"
#include "../unity/IUnityInterface.h"

namespace EffekseerPlugin
{
	enum class RendererType : int
	{
		Native = 0,
		Unity = 1,
	};

	enum class StereoRenderingType : int
	{
		// Multiple pass VR rendering.
		MultiPass = 0,
		// Single pass VR rendering ( via double-wide render texture ).
		SinglePass = 1,
		// Single pass VR rendering ( via instanced rendering ).
		Instancing = 2
	};

	const int MAX_RENDER_PATH = 128;

	extern Effekseer::Manager*	g_EffekseerManager;

	struct RenderSettings {
		int32_t cameraCullingMask = 1;
		Effekseer::Matrix44		cameraMatrix;
		Effekseer::Matrix44		projectionMatrix;
		bool					renderIntoTexture;
		void*					backgroundTexture = nullptr;

		bool					stereoEnabled;
		int						stereoRenderCount;
		StereoRenderingType     stereoRenderingType;
		Effekseer::Matrix44		leftCameraMatrix;
		Effekseer::Matrix44		leftProjectionMatrix;
		Effekseer::Matrix44		rightCameraMatrix;
		Effekseer::Matrix44		rightProjectionMatrix;
	};
	extern RenderSettings renderSettings[MAX_RENDER_PATH];

	void Array2Matrix(Effekseer::Matrix44& matrix, float matrixArray[]);

	void CalculateCameraDirectionAndPosition(const Effekseer::Matrix44& matrix, Effekseer::Vector3D& direction, Effekseer::Vector3D& position);
}

#endif
