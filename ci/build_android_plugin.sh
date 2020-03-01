
PWD = `pwd`

cd $PWD

cd Dev/Cpp/android

ndk-build clean NDK_PROJECT_PATH=./ NDK_APPLICATION_MK=jni/Application_x86.mk
ndk-build -j4  NDK_PROJECT_PATH=./ NDK_APPLICATION_MK=jni/Application_x86.mk

cp libs/x86/libEffekseerUnity.so ../../Plugin/Assets/Effekseer/Plugins/Android/libs/x86/

ndk-build clean NDK_PROJECT_PATH=./ NDK_APPLICATION_MK=jni/Application_armeabi-v7a.mk
ndk-build -j4  NDK_PROJECT_PATH=./ NDK_APPLICATION_MK=jni/Application_armeabi-v7a.mk

cp libs/armeabi-v7a/libEffekseerUnity.so ../../Plugin/Assets/Effekseer/Plugins/Android/libs/armeabi-v7a/

ndk-build clean  NDK_PROJECT_PATH=./ NDK_APPLICATION_MK=jni/Application_arm64-v8a.mk
ndk-build -j4  NDK_PROJECT_PATH=./ NDK_APPLICATION_MK=jni/Application_arm64-v8a.mk

cp libs/arm64-v8a/libEffekseerUnity.so ../../Plugin/Assets/Effekseer/Plugins/Android/libs/arm64-v8a/
