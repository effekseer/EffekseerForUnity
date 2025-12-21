cmake_minimum_required(VERSION 3.19)

if (WIN32)
    cmake_policy(SET CMP0091 NEW)
endif()

if(APPLE)
    cmake_policy(SET CMP0114 NEW)
    if(BUILD_FOR_IOS OR BUILD_FOR_IOS_SIM)
        set(CMAKE_OSX_DEPLOYMENT_TARGET "11.0" CACHE STRING "Minimum OS X deployment version" FORCE)
    else()
        set(CMAKE_OSX_DEPLOYMENT_TARGET "11.0" CACHE STRING "Minimum OS X deployment version" FORCE)
    endif()
endif()

project(EffekseerUnity  LANGUAGES C CXX)

option(SANITIZE_ENABLED "is sanitizer enabled" OFF)

option(BUILD_DX12 "Build DirectX12" OFF)
option(BUILD_FOR_IOS "Build for ios" OFF)
option(BUILD_FOR_IOS_SIM "Build for ios simulator" OFF)

# Flags
if (WIN32)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
    set(EFK_USE_MSVC_RUNTIME_LIBRARY_DLL OFF)
endif()

set(BUILD_EXAMPLES OFF CACHE INTERNAL "" FORCE)
set(BUILD_VIEWER OFF CACHE INTERNAL "" FORCE)

if(SANITIZE_ENABLED)
    set(SANITIZE_ENABLED OFF CACHE INTERNAL "" FORCE)
    if(MSVC)
        set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /fsanitize=address")
    else()
        add_compile_options(-fsanitize=undefined,address)
        add_link_options(-fsanitize=undefined,address)
    endif()
endif()

if(BUILD_FOR_IOS OR BUILD_FOR_IOS_SIM)
    set(USE_OPENGLES3 ON CACHE INTERNAL "" FORCE)
    set(BUILD_UNITYPLUGIN_FOR_IOS ON CACHE INTERNAL "" FORCE)
else()
    set(BUILD_UNITYPLUGIN ON CACHE INTERNAL "" FORCE)
endif()

add_subdirectory(Effekseer)
add_subdirectory(Cpp)

if(BUILD_FOR_IOS OR BUILD_FOR_IOS_SIM)
    target_compile_options(Effekseer PUBLIC -fembed-bitcode)
    target_compile_options(EffekseerRendererCommon PUBLIC -fembed-bitcode)
endif()