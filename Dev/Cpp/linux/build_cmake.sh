set -eu

SCRIPT_DIR=$(cd "$(dirname "$0")"; pwd)
cd "$SCRIPT_DIR/../.."

mkdir -p build_linux
cmake -B build_linux -S . -DCMAKE_BUILD_TYPE=Release -D BUILD_EXAMPLES=OFF -D USE_OPENAL=OFF
cmake --build build_linux --config Release --parallel 4

mkdir -p Plugin/Assets/Effekseer/Plugins/Linux/x86_64
cp -f build_linux/Cpp/libEffekseerUnity.so Plugin/Assets/Effekseer/Plugins/Linux/x86_64/
