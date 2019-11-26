cd `dirname $0`
source ~/emsdk/emsdk_env.sh
cd ../Dev/Cpp/webgl
emsdk activate sdk-1.38.11-64bit
emcmake cmake 
make
mkdir ../../Plugin/Assets/Effekseer/Plugins/WebGL/1.38.11-64bit/
cp libEffekseerUnity.bc ../../Plugin/Assets/Effekseer/Plugins/WebGL/1.38.11-64bit/
