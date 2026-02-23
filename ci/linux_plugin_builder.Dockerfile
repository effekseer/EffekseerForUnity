FROM ubuntu:20.04

ARG DEBIAN_FRONTEND=noninteractive
ARG CMAKE_VERSION=3.28.6

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        build-essential \
        curl \
        git \
        libgl1-mesa-dev \
        tar \
        xz-utils \
    && rm -rf /var/lib/apt/lists/*

RUN curl -fsSL "https://github.com/Kitware/CMake/releases/download/v${CMAKE_VERSION}/cmake-${CMAKE_VERSION}-linux-x86_64.tar.gz" -o /tmp/cmake.tar.gz \
    && tar -xzf /tmp/cmake.tar.gz -C /opt \
    && ln -sf "/opt/cmake-${CMAKE_VERSION}-linux-x86_64/bin/cmake" /usr/local/bin/cmake \
    && ln -sf "/opt/cmake-${CMAKE_VERSION}-linux-x86_64/bin/ctest" /usr/local/bin/ctest \
    && rm -f /tmp/cmake.tar.gz
