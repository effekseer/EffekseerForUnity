set -eu

SCRIPT_DIR=$(cd "$(dirname "$0")"; pwd)
cd "$SCRIPT_DIR/.."

sh Dev/Cpp/linux/build_cmake.sh
