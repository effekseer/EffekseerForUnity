rm -rf build
rm libEffekseerUnity.a

# build for macOS
xcodebuild -target EffekseerUnity-Mac -configuration Release build

# build for iOS (Device)
xcodebuild -target EffekseerUnity-iOS -sdk iphoneos -configuration Release build

# build for iOS (Simulator)
xcodebuild -target EffekseerUnity-iOS -sdk iphonesimulator -configuration Release build

# workaround for xcode12
lipo -remove arm64 build/Release-iphonesimulator/libEffekseerUnity.a -output build/Release-iphonesimulator/libEffekseerUnity_rmarm64.a

# Create the Universal binary
lipo -create build/Release-iphoneos/libEffekseerUnity.a build/Release-iphonesimulator/libEffekseerUnity_rmarm64.a -output libEffekseerUnity.a

# Copy to PluginProject
cp -rf build/Release/EffekseerUnity.bundle ../../Plugin/Assets/Effekseer/Plugins/
cp libEffekseerUnity.a ../../Plugin/Assets/Effekseer/Plugins/iOS/
