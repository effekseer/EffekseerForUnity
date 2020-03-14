cd `dirname $0`
source ~/emsdk/emsdk_env.sh
cd ../Dev/Cpp/webgl

mkdir build138
cd build138
emsdk activate sdk-1.38.11-64bit
emcmake cmake ..
make
mkdir ../../../Plugin/Assets/Effekseer/Plugins/WebGL/1.38.11-64bit/
cp libEffekseerUnity.bc ../../../Plugin/Assets/Effekseer/Plugins/WebGL/1.38.11-64bit/

cd ..
mkdir build137
cd build137
emsdk install sdk-1.37.3-64bit
emsdk activate sdk-1.37.3-64bit
emcmake cmake ..
make
mkdir ../../../Plugin/Assets/Effekseer/Plugins/WebGL/
cp libEffekseerUnity.bc ../../../Plugin/Assets/Effekseer/Plugins/WebGL/
cd ..

