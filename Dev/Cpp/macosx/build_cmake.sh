cd ../..
cmake -B build_macosx -S . -G "Xcode" -DCMAKE_OSX_ARCHITECTURES="arm64;x86_64" -D BUILD_EXAMPLES=OFF
cd build_macosx
cmake --build . --config Release
cmake --install . --prefix ./Install

cd ..
cmake -B build_ios -S . -G "Xcode" -D BUILD_EXAMPLES=OFF -DCMAKE_SYSTEM_NAME=iOS -DCMAKE_OSX_SYSROOT=iphoneos -D BUILD_FOR_IOS=ON
cd build_ios
cmake --build . --config Release
cmake --install . --prefix ./Install

cd ..
cmake -B build_ios_sim -S . -G "Xcode" -D BUILD_EXAMPLES=OFF -DCMAKE_SYSTEM_NAME=iOS -DCMAKE_OSX_SYSROOT=iphonesimulator -D BUILD_FOR_IOS_SIM=ON
cd build_ios_sim
cmake --build . --config Release
cmake --install . --prefix ./Install

cd ../Cpp/macosx

# workaround for xcode12
lipo -remove arm64 ../../build_ios_sim/Install/lib/libEffekseerUnity.a -output ../../build_ios_sim/Install/lib/libEffekseerUnity_rmarm64.a
lipo -remove arm64 ../../build_ios_sim/Install/lib/libEffekseerRendererCommon.a -output ../../build_ios_sim/Install/lib/libEffekseerRendererCommon_rmarm64.a
lipo -remove arm64 ../../build_ios_sim/Install/lib/libEffekseer.a -output ../../build_ios_sim/Install/lib/libEffekseer_rmarm64.a

# Create the Universal binary
libtool -static -o libEffekseerUnity_ios.a ../../build_ios/Install/lib/libEffekseer.a ../../build_ios/Install/lib/libEffekseerRendererCommon.a ../../build_ios/Install/lib/libEffekseerUnity.a
libtool -static -o libEffekseerUnity_ios_sim.a ../../build_ios_sim/Install/lib/libEffekseer_rmarm64.a ../../build_ios_sim/Install/lib/libEffekseerRendererCommon_rmarm64.a ../../build_ios_sim/Install/lib/libEffekseerUnity_rmarm64.a

lipo -create libEffekseerUnity_ios.a libEffekseerUnity_ios_sim.a -output libEffekseerUnity.a

# Copy to PluginProject
rm -rf ../../Plugin/Assets/Effekseer/Plugins/EffekseerUnity.bundle
mkdir -p ../../Plugin/Assets/Effekseer/Plugins/macOS
cp -f ../../build_macosx/Install/lib/EffekseerUnity.dylib ../../Plugin/Assets/Effekseer/Plugins/macOS/
cp libEffekseerUnity.a ../../Plugin/Assets/Effekseer/Plugins/iOS/
