include $(CLEAR_VARS)

LOCAL_PATH := $(call my-dir)

LOCAL_ARM_MODE  := arm
LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := libEffekseerUnity
LOCAL_CFLAGS    := -O2 -D__EFFEKSEER_RENDERER_GLES3__ -D__EFFEKSEER_RENDERER_INTERNAL_LOADER__ -D__EFFEKSEER_NETWORK_ENABLED__ -std=c++17
LOCAL_LDLIBS    := -landroid -lEGL -lGLESv3
LOCAL_LDFLAGS   += -Wl,-z,max-page-size=16384

LOCAL_C_INCLUDES += \
	$(LOCAL_PATH)/../../Effekseer/Dev/Cpp/Effekseer \
	$(LOCAL_PATH)/../../Effekseer/Dev/Cpp/EffekseerRendererCommon \
	$(LOCAL_PATH)/../../Effekseer/Dev/Cpp/EffekseerRendererGL \
	$(LOCAL_PATH)/../../Effekseer/Dev/Cpp/3rdParty/flatbuffers/include

LOCAL_SRC_FILES := \
	$(LOCAL_PATH)/../common/EffekseerPluginCommon.cpp \
	$(LOCAL_PATH)/../common/EffekseerPluginNetwork.cpp \
	$(LOCAL_PATH)/../common/EffekseerPluginModel.cpp \
	$(LOCAL_PATH)/../common/EffekseerPluginCurve.cpp \
	$(LOCAL_PATH)/../common/EffekseerPluginMaterial.cpp \
	$(LOCAL_PATH)/../common/EffekseerPluginSound.cpp \
	$(LOCAL_PATH)/../common/EffekseerPlugin.cpp \
	$(LOCAL_PATH)/../common/MemoryFile.cpp \
	$(LOCAL_PATH)/../renderer/EffekseerRendererImplemented.cpp \
	$(LOCAL_PATH)/../renderer/EffekseerRendererModelLoader.cpp \
	$(LOCAL_PATH)/../renderer/EffekseerRendererMaterialLoader.cpp \
	$(LOCAL_PATH)/../renderer/EffekseerRendererRenderState.cpp \
	$(LOCAL_PATH)/../renderer/EffekseerRendererShader.cpp \
	$(LOCAL_PATH)/../renderer/EffekseerRendererTextureLoader.cpp \
	$(LOCAL_PATH)/../graphicsAPI/EffekseerPluginGraphics.cpp \
	$(LOCAL_PATH)/../graphicsAPI/EffekseerPluginGraphicsUnity.cpp \
	$(LOCAL_PATH)/../graphicsAPI/EffekseerPluginGraphicsGL.cpp

LIB_SRC_PATH := $(LOCAL_PATH)/../../Effekseer/Dev/Cpp/Effekseer/Effekseer
LOCAL_SRC_FILES += \
	$(LIB_SRC_PATH)/Network/Effekseer.Client.cpp \
	$(LIB_SRC_PATH)/Network/Effekseer.Server.cpp \
	$(LIB_SRC_PATH)/Network/Effekseer.Session.cpp \
	$(LIB_SRC_PATH)/Network/Effekseer.Socket.cpp \
	$(LIB_SRC_PATH)/Effekseer.Color.cpp \
	$(LIB_SRC_PATH)/Effekseer.CurveLoader.cpp \
	$(LIB_SRC_PATH)/Effekseer.DefaultEffectLoader.cpp \
	$(LIB_SRC_PATH)/Effekseer.DefaultFile.cpp \
	$(LIB_SRC_PATH)/Effekseer.Effect.cpp \
	$(LIB_SRC_PATH)/Effekseer.EffectNode.cpp \
	$(LIB_SRC_PATH)/Effekseer.EffectNodeModel.cpp \
	$(LIB_SRC_PATH)/Effekseer.EffectNodeRibbon.cpp \
	$(LIB_SRC_PATH)/Effekseer.EffectNodeRing.cpp \
	$(LIB_SRC_PATH)/Effekseer.EffectNodeRoot.cpp \
	$(LIB_SRC_PATH)/Effekseer.EffectNodeSprite.cpp \
	$(LIB_SRC_PATH)/Effekseer.EffectNodeTrack.cpp \
	$(LIB_SRC_PATH)/Effekseer.FCurves.cpp \
	$(LIB_SRC_PATH)/Effekseer.Instance.cpp \
	$(LIB_SRC_PATH)/Effekseer.InstanceContainer.cpp \
	$(LIB_SRC_PATH)/Effekseer.InstanceGlobal.cpp \
	$(LIB_SRC_PATH)/Effekseer.InstanceGroup.cpp \
	$(LIB_SRC_PATH)/Effekseer.InstanceChunk.cpp \
	$(LIB_SRC_PATH)/Effekseer.InternalScript.cpp \
	$(LIB_SRC_PATH)/Effekseer.Manager.cpp \
	$(LIB_SRC_PATH)/Effekseer.Matrix43.cpp \
	$(LIB_SRC_PATH)/Effekseer.Matrix44.cpp \
	$(LIB_SRC_PATH)/Effekseer.Random.cpp \
	$(LIB_SRC_PATH)/Effekseer.RectF.cpp \
	$(LIB_SRC_PATH)/Effekseer.Resource.cpp \
	$(LIB_SRC_PATH)/Effekseer.ResourceManager.cpp \
	$(LIB_SRC_PATH)/Effekseer.Setting.cpp \
	$(LIB_SRC_PATH)/Effekseer.Vector2D.cpp \
	$(LIB_SRC_PATH)/Effekseer.Vector3D.cpp \
	$(LIB_SRC_PATH)/Effekseer.WorkerThread.cpp \
	$(LIB_SRC_PATH)/ForceField/Effekseer.ForceFields.cpp \
	$(LIB_SRC_PATH)/IO/Effekseer.EfkEfcFactory.cpp \
	$(LIB_SRC_PATH)/Material/Effekseer.CompiledMaterial.cpp \
	$(LIB_SRC_PATH)/Material/Effekseer.MaterialCompiler.cpp \
	$(LIB_SRC_PATH)/Material/Effekseer.MaterialFile.cpp \
	$(LIB_SRC_PATH)/Model/Effekseer.Model.cpp \
	$(LIB_SRC_PATH)/Model/Effekseer.ModelLoader.cpp \
	$(LIB_SRC_PATH)/Model/Effekseer.ProceduralModelGenerator.cpp \
	$(LIB_SRC_PATH)/Model/Effekseer.PointCacheGenerator.cpp \
	$(LIB_SRC_PATH)/Model/Effekseer.SplineGenerator.cpp \
	$(LIB_SRC_PATH)/Noise/Effekseer.CurlNoise.cpp \
	$(LIB_SRC_PATH)/Parameter/Effekseer.Collisions.cpp \
	$(LIB_SRC_PATH)/Parameter/Effekseer.Easing.cpp \
	$(LIB_SRC_PATH)/Parameter/Effekseer.GpuParticlesParameter.cpp \
	$(LIB_SRC_PATH)/Parameter/Effekseer.KillRules.cpp \
	$(LIB_SRC_PATH)/Parameter/Effekseer.Parameters.cpp \
	$(LIB_SRC_PATH)/Parameter/Effekseer.Rotation.cpp \
	$(LIB_SRC_PATH)/Parameter/Effekseer.Trigger.cpp \
	$(LIB_SRC_PATH)/SIMD/Mat43f.cpp \
	$(LIB_SRC_PATH)/SIMD/Mat44f.cpp \
	$(LIB_SRC_PATH)/SIMD/Utils.cpp \
	$(LIB_SRC_PATH)/Utils/Effekseer.CustomAllocator.cpp

LIB_SRC_PATH := $(LOCAL_PATH)/../../Effekseer/Dev/Cpp/EffekseerMaterialCompiler
LOCAL_SRC_FILES += \
	$(LIB_SRC_PATH)/OpenGL/EffekseerMaterialCompilerGL.cpp \
	$(LIB_SRC_PATH)/GLSLGenerator/ShaderGenerator.cpp \
	$(LIB_SRC_PATH)/Common/ShaderGeneratorCommon.cpp

LIB_SRC_PATH := $(LOCAL_PATH)/../../Effekseer/Dev/Cpp/EffekseerRendererCommon/EffekseerRendererCommon
LOCAL_SRC_FILES += \
	$(LIB_SRC_PATH)/GraphicsDeviceCPU.cpp \
	$(LIB_SRC_PATH)/TextureLoader.cpp \
	$(LIB_SRC_PATH)/ModelLoader.cpp \
	$(LIB_SRC_PATH)/VertexBuffer.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.CommonUtils.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.DDSTextureLoader.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.GpuParticles.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.ModelRendererBase.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.PngTextureLoader.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.Renderer.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.Renderer_Impl.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.RenderStateBase.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.RibbonRendererBase.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.RingRendererBase.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.SpriteRendererBase.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.TGATextureLoader.cpp \
	$(LIB_SRC_PATH)/EffekseerRenderer.TrackRendererBase.cpp

LIB_SRC_PATH := $(LOCAL_PATH)/../../Effekseer/Dev/Cpp/EffekseerRendererGL/EffekseerRendererGL
LOCAL_SRC_FILES += \
	$(LIB_SRC_PATH)/EffekseerRendererGL.DeviceObject.cpp \
	$(LIB_SRC_PATH)/EffekseerRendererGL.GLExtension.cpp \
	$(LIB_SRC_PATH)/EffekseerRendererGL.GpuTimer.cpp \
	$(LIB_SRC_PATH)/EffekseerRendererGL.MaterialLoader.cpp \
	$(LIB_SRC_PATH)/EffekseerRendererGL.ModelRenderer.cpp \
	$(LIB_SRC_PATH)/EffekseerRendererGL.Renderer.cpp \
	$(LIB_SRC_PATH)/EffekseerRendererGL.RenderState.cpp \
	$(LIB_SRC_PATH)/EffekseerRendererGL.Shader.cpp \
	$(LIB_SRC_PATH)/GraphicsDevice.cpp

include $(BUILD_SHARED_LIBRARY)
