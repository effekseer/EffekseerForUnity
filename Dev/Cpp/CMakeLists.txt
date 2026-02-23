cmake_minimum_required(VERSION 3.18)

project(EffekseerUnity  LANGUAGES C CXX)

include(ExternalProject)
include(cmake/FilterFolder.cmake)
include(cmake/ClangFormat.cmake)

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
        ${effekseer_src}
        ${effekseer_dx11_src}
        ${effekseer_gl_src})

    if(BUILD_DX12)
        list(APPEND srcs 
        graphicsAPI/EffekseerPluginGraphicsLLGI.h
        graphicsAPI/EffekseerPluginGraphicsLLGI.cpp
        ${effekseer_dx12_src})
    endif()

    add_library(EffekseerUnity SHARED
        ${srcs}
    )

    target_link_libraries(EffekseerUnity PRIVATE
        Effekseer
        EffekseerRendererGL
        EffekseerRendererDX11)

    if(BUILD_DX12)
        target_link_libraries(EffekseerUnity PRIVATE
            EffekseerRendererDX12
            LLGI)
        target_compile_definitions(EffekseerUnity PRIVATE _DX12)
    endif()
endif()

if(APPLE)
    set(srcs
        ${effekseer_src})

    if(BUILD_FOR_IOS OR BUILD_FOR_IOS_SIM)
        add_library(EffekseerUnity STATIC
            ${srcs}
        )
    else()
        list(APPEND srcs 
            ${effekseer_gl_src})
        add_library(EffekseerUnity SHARED
            ${srcs}
        )
        # Keep the binary name as EffekseerUnity.dylib for Unity DllImport("EffekseerUnity").
        set_target_properties(EffekseerUnity PROPERTIES PREFIX "")
    endif()

    target_link_libraries(EffekseerUnity PRIVATE
        Effekseer)

    if(NOT BUILD_FOR_IOS AND NOT BUILD_FOR_IOS_SIM)
        find_package(OpenGL REQUIRED)
        target_link_libraries(EffekseerUnity PRIVATE
            EffekseerRendererGL
            ${OPENGL_LIBRARIES})
    endif()

    set_target_properties(EffekseerUnity PROPERTIES XCODE_ATTRIBUTE_ENABLE_HARDENED_RUNTIME YES)
    target_compile_options(EffekseerUnity PUBLIC -fembed-bitcode)
endif()

if(UNIX AND NOT APPLE AND NOT ANDROID AND NOT EMSCRIPTEN)
    set(srcs
        ${effekseer_src}
        ${effekseer_gl_src})

    add_library(EffekseerUnity SHARED
        ${srcs}
    )

    target_link_libraries(EffekseerUnity PRIVATE
        Effekseer
        EffekseerRendererGL)

    find_package(OpenGL REQUIRED)
    target_link_libraries(EffekseerUnity PRIVATE
        ${OPENGL_LIBRARIES})
endif()

list(APPEND EFK_INCLUDES ../Effekseer/Dev/Cpp/)
list(APPEND EFK_INCLUDES ../Effekseer/Dev/Cpp/Effekseer/)
list(APPEND EFK_INCLUDES ../Effekseer/Dev/Cpp/EffekseerRendererCommon/)

target_compile_definitions(EffekseerUnity PRIVATE __EFFEKSEER_FROM_MAIN_CMAKE__)
target_include_directories(EffekseerUnity PRIVATE ${EFK_INCLUDES})
target_link_directories(EffekseerUnity PRIVATE ${EFK_LIBRARY_DIRECTORIES})
target_compile_features(EffekseerUnity PUBLIC cxx_std_17)

FilterFolder("${srcs}")

install(
    TARGETS ${PROJECT_NAME}
    EXPORT ${PROJECT_NAME}-export
    INCLUDES DESTINATION include
    ARCHIVE DESTINATION lib
    LIBRARY DESTINATION lib)

install(
    EXPORT ${PROJECT_NAME}-export
    FILE ${PROJECT_NAME}-config.cmake
    DESTINATION lib/cmake
    EXPORT_LINK_INTERFACE_LIBRARIES)
