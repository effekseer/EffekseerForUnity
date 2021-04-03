cmake_minimum_required(VERSION 3.18)

if (WIN32)
    cmake_policy(SET CMP0091 NEW)
endif()

project(EffekseerUnity  LANGUAGES C CXX)

include(ExternalProject)

option(BUILD_DX12 "Build DirectX12" OFF)

# Flags
if (WIN32)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

ExternalProject_Add(
    ExternalProject_effekseer
    SOURCE_DIR      ${CMAKE_CURRENT_SOURCE_DIR}/../../../Effekseer
    PREFIX          ${CMAKE_CURRENT_BINARY_DIR}/Build/Effekseer
    INSTALL_DIR     ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer
    CMAKE_ARGS      -DCMAKE_INSTALL_PREFIX=${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer
                    -DEFK_USE_MSVC_RUNTIME_LIBRARY_DLL=false
                    -DCMAKE_POSITION_INDEPENDENT_CODE:BOOL=true
                    -DBUILD_VIEWER:BOOL=OFF
                    -DBUILD_EXAMPLES:BOOL=OFF
                    -DBUILD_UNITYPLUGIN:BOOL=ON
    )
    
list(APPEND EFK_INCLUDES ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer/include/)
list(APPEND EFK_INCLUDES ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer/include/Effekseer)
list(APPEND EFK_INCLUDES ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer/include/EffekseerRendererCommon)
list(APPEND EFK_LIBRARY_DIRECTORIES ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer/lib)

file(GLOB effekseer_src
    common/*.cpp
    renderer/*.cpp
    common/*.h
    renderer/*.h
)

list(APPEND effekseer_src
    graphicsAPI/EffekseerPluginGraphics.h
    graphicsAPI/EffekseerPluginGraphics.cpp
    graphicsAPI/EffekseerPluginGraphicsUnity.h
    graphicsAPI/EffekseerPluginGraphicsUnity.cpp
)

set(effekseer_dx11_src
    graphicsAPI/EffekseerPluginGraphicsDX11.h
    graphicsAPI/EffekseerPluginGraphicsDX11.cpp
)

set(effekseer_dx12_src
    graphicsAPI/EffekseerPluginGraphicsDX12.h
    graphicsAPI/EffekseerPluginGraphicsDX12.cpp
)

set(effekseer_gl_src
    graphicsAPI/EffekseerPluginGraphicsGL.h
    graphicsAPI/EffekseerPluginGraphicsGL.cpp
)

if(WIN32)
    set(srcs
        windows/EffekseerUnity.def
        graphicsAPI/EffekseerPluginGraphicsLLGI.h
        graphicsAPI/EffekseerPluginGraphicsLLGI.cpp
        ${effekseer_src}
        ${effekseer_dx11_src}
        ${effekseer_gl_src})

    if(BUILD_DX12)
        list(APPEND srcs ${effekseer_dx12_src})
    endif()

    add_library(EffekseerUnity SHARED
        ${srcs}
    )

    target_link_libraries(EffekseerUnity PRIVATE
        Effekseer
        EffekseerRendererGL
        EffekseerRendererDX11)

    target_compile_definitions(EffekseerUnity PRIVATE __EFFEKSEER_FROM_MAIN_CMAKE__)
    target_include_directories(EffekseerUnity PRIVATE ${EFK_INCLUDES})
    target_link_directories(EffekseerUnity PRIVATE ${EFK_LIBRARY_DIRECTORIES})

    add_dependencies(EffekseerUnity ExternalProject_effekseer)

    if(BUILD_DX12)
        target_link_libraries(EffekseerUnity PRIVATE
            EffekseerRendererDX12
            LLGI)
        target_compile_definitions(EffekseerUnity PRIVATE _DX12)
    endif()
endif()

