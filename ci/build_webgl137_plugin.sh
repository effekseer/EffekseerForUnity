cd `dirname $0`
source ~/emsdk/emsdk_env.sh
cd ../Dev/Cpp/webgl

mkdir build137
cd build137
emsdk install sdk-1.37.40-64bit
emsdk activate sdk-1.37.40-64bit
emcmake cmake ..
make
mkdir ../../../Plugin/Assets/Effekseer/Plugins/WebGL/
cp libEffekseerUnity.bc ../../../Plugin/Assets/Effekseer/Plugins/WebGL/

