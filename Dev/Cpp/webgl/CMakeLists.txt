project(Effekseer)
cmake_minimum_required(VERSION 3.15)


string(APPEND CMAKE_EXE_LINKER_FLAGS " -s WASM=1")

set(
	INCLUDE_DIR
	"${PROJECT_SOURCE_DIR}/../unity"
	"${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer"
	"${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/EffekseerRendererCommon"
	"${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/EffekseerRendererGL")
	
include_directories (${INCLUDE_DIR}) 

# make OpenGL ES enabled
add_definitions(-D__EFFEKSEER_RENDERER_GLES2__)
add_definitions(-D__DISABLED_DEFAULT_TEXTURE_LOADER__)

file(GLOB effekseer_src
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/Backend/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/Culling/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/ForceField/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/IO/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/Material/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/Model/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/Network/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/Noise/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/Parameter/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/SIMD/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/Effekseer/Effekseer/Utils/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/EffekseerRendererCommon/*.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/EffekseerRendererGL/EffekseerRenderer/*.cpp
	${PROJECT_SOURCE_DIR}/../common/*.cpp
	${PROJECT_SOURCE_DIR}/../renderer/*.cpp
	${PROJECT_SOURCE_DIR}/../graphicsAPI/EffekseerPluginGraphics.cpp
	${PROJECT_SOURCE_DIR}/../graphicsAPI/EffekseerPluginGraphicsGL.cpp
	${PROJECT_SOURCE_DIR}/../graphicsAPI/EffekseerPluginGraphicsUnity.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/EffekseerMaterialCompiler/OpenGL/EffekseerMaterialCompilerGL.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/EffekseerMaterialCompiler/GLSLGenerator/ShaderGenerator.cpp
	${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/EffekseerMaterialCompiler/Common/ShaderGeneratorCommon.cpp)

# INTERNAL_LOADER
list(REMOVE_ITEM effekseer_src "${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/EffekseerRendererCommon/EffekseerRenderer.DXTK.DDSTextureLoader.cpp")
list(REMOVE_ITEM effekseer_src "${PROJECT_SOURCE_DIR}/../../Effekseer/Dev/Cpp/EffekseerRendererCommon/EffekseerRenderer.PngTextureLoader.cpp")

# Specify extentions
set(CMAKE_STATIC_LIBRARY_SUFFIX ".bc")
set(CMAKE_SHARED_LIBRARY_SUFFIX ".bc")

add_library(EffekseerUnity SHARED ${effekseer_src})
target_compile_features(EffekseerUnity PUBLIC cxx_std_17)
