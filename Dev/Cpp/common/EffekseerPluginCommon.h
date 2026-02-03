
#ifndef __EFFEKSEER_PLUGIN_COMMON_H__
#define __EFFEKSEER_PLUGIN_COMMON_H__

#include <array>
#include <vector>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include "Effekseer.h"
#endif

#include <Effekseer/Model/Effekseer.ProceduralModelGenerator.h>

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

class RenderThreadEvent
{
private:
	std::mutex mtx_;
	std::vector<std::function<void()>> events_;
	static std::shared_ptr<RenderThreadEvent> instance_;

public:
	~RenderThreadEvent();

	static void Initialize();

	static void Terminate();

	static std::shared_ptr<RenderThreadEvent> GetInstance();

	void AddEvent(const std::function<void()>& e);

	void Execute();
};

class MultiThreadedEffekseerManager
{
public:
	struct PlayEffectParameters
	{
		std::array<float, 3> Position;
		std::array<float, 4> Rotation;
		std::array<float, 3> Scale;
		int32_t Visible;
		float Speed;
	};

private:
	enum class CommandType : int32_t
	{
		Update,
		Play,
		SetTimeScaleByGroup,
		StopAllEffects,
		SetPausedToAllEffects,
		UpdateHandle,
		UpdateHandleToMoveToFrame,
		Stop,
		StopRoot,
		SendTrigger,
		SetVisibility,
		SetPause,
		SetSpeed,
		SetPosition,
		SetRotation,
		SetScale,
		SetTargetLocation,
		SetColor,
		SetDynamicInput,
		SetLayer,
		SetGroupMask,
	};

	struct Command
	{
		CommandType Type;
		int Handle;

		union
		{
			struct
			{
				void* EffectPtr;
				std::array<float, 3> Position;
				std::array<float, 4> Rotation;
				std::array<float, 3> Scale;
				int32_t Visible;
				float Speed;
			} Play;

			struct
			{
				float DeltaFrame;
			} Update;

			struct
			{
				int Value;
			} IntValue;

			struct
			{
				int64_t Value;
			} Int64Value;

			struct
			{
				float Value;
			} FloatValue;

			struct
			{
				bool Value;
			} BoolValue;

			struct
			{
				float Value;
				int Index;
			} FloatValueIndex;

			struct
			{
				float Values[4];
			} FloatArrayValue;

			struct
			{
				int Values[4];
			} IntArrayValue;

			struct
			{
				int64_t GroupMask;
				float TimeScale;
			} SetTimeScaleByGroup;
		};
	};

	struct EffectState
	{
		Effekseer::EffectRef Effect;
		bool Visible = true;
		bool Paused = false;
		float Speed = 1.0f;
		int InstanceCount = 0;
		std::array<float, 4> DynamicInputs;
	};

	Effekseer::ManagerRef manager_;
	std::vector<Command> commands_;
	std::vector<Command> threadCommands_;
	std::vector<int32_t> removingIds_;
	int32_t restInstanceCount_ = 0;
	int32_t cameraCullingMaskToShowAllEffects_ = 0;
	int32_t nextInternalHandle_ = 1;
	std::unordered_map<int32_t, EffectState> internalHandleStates_;
	std::unordered_map<int32_t, Effekseer::Handle> internalHandleToHandle_;
	std::mutex mtx_;

	static std::shared_ptr<MultiThreadedEffekseerManager> instance_;
	static std::mutex instance_mtx_;

	void PushCommand(const Command& cmd);

public:
	MultiThreadedEffekseerManager(int maxInstances);
	~MultiThreadedEffekseerManager();
	void Apply();
	void Update(float deltaFrame);
	void StopAllEffects();
	void SetPausedToAllEffects(bool paused);
	void UpdateHandle(int handle, float deltaFrame);
	void UpdateHandleToMoveToFrame(int handle, float frame);
	int32_t PlayEffect(void* effectPtr, const PlayEffectParameters& param);
	void StopEffect(int32_t handle);
	void StopRootEffect(int32_t handle);
	void SendTrigger(int32_t handle, int32_t index);
	void SetVisibility(int32_t handle, bool visible);
	void SetPaused(int32_t handle, bool paused);
	void SetSpeed(int32_t handle, float speed);
	void SetPosition(int32_t handle, float x, float y, float z);
	void SetRotation(int32_t handle, float x, float y, float z, float angle);
	void SetScale(int32_t handle, float x, float y, float z);
	void SetTargetLocation(int32_t handle, float x, float y, float z);
	void SetColor(int32_t handle, int r, int g, int b, int a);
	void SetDynamicInput(int32_t handle, int index, float value);
	void SetLayer(int32_t handle, int32_t layer);
	void SetGroupMask(int32_t handle, int64_t groupMask);
	void SetTimeScaleByGroup(int64_t groupMask, float timeScale);

	bool Exists(int32_t handle);
	bool GetVisibility(int32_t handle);
	bool GetPaused(int32_t handle);
	float GetSpeed(int32_t handle);
	int GetInstanceCount(int32_t handle);
	int GetRestInstanceCount();
	int GetCameraCullingMaskToShowAllEffects();
	float GetDynamicInput(int32_t handle, int index);
	const char16_t* GetName(int32_t handle);

	int GetEffectHandles(int* dst, int count);

	Effekseer::ManagerRef& GetManager();

	static void Initialize(int maxInstances);

	static void Terminate();

	static std::shared_ptr<MultiThreadedEffekseerManager> GetInstance();
};

} // namespace EffekseerPlugin

#endif
