
#ifndef __EFFEKSEER_PLUGIN_COMMON_H__
#define __EFFEKSEER_PLUGIN_COMMON_H__

#include <array>
#include <vector>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include "Effekseer.h"
#endif

#include <Effekseer/Model/ProceduralModelGenerator.h>

#include "../unity/IUnityInterface.h"

namespace EffekseerPlugin
{

enum class ExternalTextureType : int
{
	Background,
	Depth,
	Max,
};

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

struct RenderSettings
{
	int32_t cameraCullingMask = 1;
	Effekseer::Matrix44 cameraMatrix;
	Effekseer::Matrix44 projectionMatrix;
	bool renderIntoTexture = false;
	std::array<void*, static_cast<int>(ExternalTextureType::Max)> externalTextures;

	bool stereoEnabled = false;
	int stereoRenderCount = 1;
	StereoRenderingType stereoRenderingType;
	Effekseer::Matrix44 leftCameraMatrix;
	Effekseer::Matrix44 leftProjectionMatrix;
	Effekseer::Matrix44 rightCameraMatrix;
	Effekseer::Matrix44 rightProjectionMatrix;

	RenderSettings() { externalTextures.fill(nullptr); }
};
extern RenderSettings renderSettings[MAX_RENDER_PATH];

void Array2Matrix(Effekseer::Matrix44& matrix, float matrixArray[]);

void CalculateCameraDirectionAndPosition(const Effekseer::Matrix44& matrix, Effekseer::Vector3D& direction, Effekseer::Vector3D& position);

using ProceduralModelGeneratorGenerate = void*(UNITY_INTERFACE_API*)(Effekseer::Model::Vertex* vertecies,
																	int verteciesCount,
																	Effekseer::Model::Face* faces,
																	int facesCount);
using ProceduralModelGeneratorUngenerate = void(UNITY_INTERFACE_API*)(void* modelPointer);

} // namespace EffekseerPlugin

#endif
