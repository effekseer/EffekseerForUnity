
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

using GetUnityIDFromPath = int(UNITY_INTERFACE_API*)(const char16_t* path);

template <typename T> class IDtoResourceTable
{
	struct Resource
	{
		int referenceCount;
		T resource;
		void* nativePtr;
	};
	std::map<int, Resource> resources_;

public:
	bool TryLoad(int id, T& value)
	{
		auto it = resources_.find(id);
		if (it != resources_.end())
		{
			it->second.referenceCount++;
			value = it->second.resource;
			return true;
		}

		return false;
	}

	void Register(int id, T value, void* nativePtr) { resources_.insert(std::make_pair(id, Resource{1, value, nativePtr})); }

	bool Unload(T value, int& id, void*& nativePtr)
	{
		auto it = std::find_if(
			resources_.begin(), resources_.end(), [value](const std::pair<int, Resource>& pair) { return pair.second.resource == value; });
		if (it == resources_.end())
		{
			return false;
		}

		it->second.referenceCount--;
		if (it->second.referenceCount <= 0)
		{
			id = it->first;
			nativePtr = it->second.nativePtr;
			resources_.erase(it);
			return true;
		}

		return false;
	}
};

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

struct ExternalTextureProperty
{
	Effekseer::Backend::TextureRef Texture;
	void* OriginalPtr = nullptr;

	void Reset()
	{
		Texture.Reset();
		OriginalPtr = nullptr;
	}
};

struct RenderSettings
{
	int32_t id = 0;
	int32_t cameraCullingMask = 1;
	Effekseer::Matrix44 cameraMatrix;
	Effekseer::Matrix44 projectionMatrix;
	bool renderIntoTexture = false;
	std::array<ExternalTextureProperty, static_cast<int>(ExternalTextureType::Max)> externalTextures;

	bool stereoEnabled = false;
	int stereoRenderCount = 1;
	StereoRenderingType stereoRenderingType = StereoRenderingType::MultiPass;
	Effekseer::Matrix44 leftCameraMatrix;
	Effekseer::Matrix44 leftProjectionMatrix;
	Effekseer::Matrix44 rightCameraMatrix;
	Effekseer::Matrix44 rightProjectionMatrix;

	Effekseer::Backend::TextureFormatType renderTargetType = Effekseer::Backend::TextureFormatType::R8G8B8A8_UNORM;
	Effekseer::Backend::TextureFormatType depthTargetType = Effekseer::Backend::TextureFormatType::D32S8;
	int32_t screenWidth = 0;
	int32_t screenHeight = 0;

	RenderSettings()
	{
		for (auto& t : externalTextures)
		{
			t.Reset();
		}
	}
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
