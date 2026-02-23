set -eu

SCRIPT_DIR=$(cd "$(dirname "$0")"; pwd)
cd "$SCRIPT_DIR/../.."

BUILD_DIR=${BUILD_DIR:-build_linux}

mkdir -p "$BUILD_DIR"
cmake -B "$BUILD_DIR" -S . -DCMAKE_BUILD_TYPE=Release -D BUILD_EXAMPLES=OFF -D USE_OPENAL=OFF
cmake --build "$BUILD_DIR" --config Release --parallel 4

mkdir -p Plugin/Assets/Effekseer/Plugins/Linux/x86_64
cp -f "$BUILD_DIR"/Cpp/libEffekseerUnity.so Plugin/Assets/Effekseer/Plugins/Linux/x86_64/
