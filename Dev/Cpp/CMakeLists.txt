project(EffekseerUnity  LANGUAGES C CXX)

include(ExternalProject)
cmake_minimum_required(VERSION 3.1)

# Flags
if (WIN32)
    foreach (flag CMAKE_C_FLAGS
        CMAKE_C_FLAGS_DEBUG
        CMAKE_C_FLAGS_RELEASE
        CMAKE_CXX_FLAGS
        CMAKE_CXX_FLAGS_DEBUG
        CMAKE_CXX_FLAGS_RELEASE)

        if (${flag} MATCHES "/MD")
            string(REGEX REPLACE "/MD" "/MT" ${flag} "${${flag}}")
        endif()
        if (${flag} MATCHES "/MDd")
            string(REGEX REPLACE "/MDd" "/MTd" ${flag} "${${flag}}")
        endif()
    endforeach()
endif()

ExternalProject_Add(
    ExternalProject_effekseer
    SOURCE_DIR      ${CMAKE_CURRENT_SOURCE_DIR}/../../../Effekseer
    PREFIX          ${CMAKE_CURRENT_BINARY_DIR}/Build/Effekseer
    INSTALL_DIR     ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer
    CMAKE_ARGS      -DCMAKE_INSTALL_PREFIX=${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer
                    -DEFK_USE_MSVC_RUNTIME_LIBRARY_DLL=${USE_MSVC_RUNTIME_LIBRARY_DLL}
                    -DCMAKE_POSITION_INDEPENDENT_CODE:BOOL=true
                    -DBUILD_VIEWER:BOOL=OFF
                    -DBUILD_EXAMPLES:BOOL=OFF
                    -DBUILD_UNITYPLUGIN:BOOL=ON
    )
    
list(APPEND EFK_INCLUDES ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer/include/)
list(APPEND EFK_INCLUDES ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer/include/Effekseer)
list(APPEND EFK_INCLUDES ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer/include/EffekseerRendererCommon)
list(APPEND EFK_LIBRARY_DIRECTORIES ${CMAKE_CURRENT_BINARY_DIR}/Install/Effekseer/lib)

add_definitions(-D__EFFEKSEER_FROM_MAIN_CMAKE__)

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
    graphicsAPI/EffekseerPluginGraphicsGL.h
    graphicsAPI/EffekseerPluginGraphicsGL.cpp
)

if(WIN32)

    list(APPEND effekseer_src
        graphicsAPI/EffekseerPluginGraphicsDX11.h
        graphicsAPI/EffekseerPluginGraphicsDX11.cpp
        windows/EffekseerUnity.def
    )

endif()

add_library(EffekseerUnity SHARED
    ${effekseer_src}
)

target_include_directories(EffekseerUnity PRIVATE ${EFK_INCLUDES})
target_link_directories(EffekseerUnity PRIVATE ${EFK_LIBRARY_DIRECTORIES})

target_link_libraries(EffekseerUnity PRIVATE
    Effekseer
    EffekseerRendererGL)

if(WIN32)
    target_link_libraries(EffekseerUnity PRIVATE
        EffekseerRendererDX9
        EffekseerRendererDX11)
endif()

add_dependencies(EffekseerUnity ExternalProject_effekseer)
