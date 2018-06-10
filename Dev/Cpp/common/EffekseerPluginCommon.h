
#ifndef	__EFFEKSEER_PLUGIN_COMMON_H__
#define __EFFEKSEER_PLUGIN_COMMON_H__

#include <vector>
#include "Effekseer.h"
#include "IUnityInterface.h"

//#define UNITY_INTERFACE_EXPORT	// Must export by .def!!
#define UNITY_API UNITY_INTERFACE_API

namespace EffekseerPlugin
{
	const int MAX_RENDER_PATH = 128;

	extern Effekseer::Manager*	g_EffekseerManager;

	struct RenderSettings {
		Effekseer::Matrix44		cameraMatrix;
		Effekseer::Matrix44		projectionMatrix;
		bool					renderIntoTexture;
		void*					backgroundTexture = nullptr;

		bool					stereoEnabled;
		int						stereoRenderCount;
		Effekseer::Matrix44		leftCameraMatrix;
		Effekseer::Matrix44		leftProjectionMatrix;
		Effekseer::Matrix44		rightCameraMatrix;
		Effekseer::Matrix44		rightProjectionMatrix;
	};
	extern RenderSettings renderSettings[MAX_RENDER_PATH];

	void Array2Matrix(Effekseer::Matrix44& matrix, float matrixArray[]);
}

#endif
