#!/usr/bin/env bash
set -eu

SCRIPT_DIR=$(cd "$(dirname "$0")"; pwd)
ROOT_DIR=$(cd "$SCRIPT_DIR/.."; pwd)

IMAGE_NAME="effekseer-unity-linux-builder:ubuntu20.04"
BUILD_DIR_NAME="build_linux_docker_$(id -u)"

docker build \
  --file "$SCRIPT_DIR/linux_plugin_builder.Dockerfile" \
  --tag "$IMAGE_NAME" \
  "$ROOT_DIR"

docker run --rm \
  --user "$(id -u):$(id -g)" \
  --env HOME=/tmp \
  --env BUILD_DIR="$BUILD_DIR_NAME" \
  --volume "$ROOT_DIR:/work" \
  --workdir /work \
  "$IMAGE_NAME" \
  bash -lc '
    set -eu
    # Git on Actions-mounted volumes can be treated as "dubious ownership" in containers.
    git config --global --add safe.directory "*"
    sh scripts/clone_dependencies.sh
    sh ci/build_linux_plugin.sh
  '
