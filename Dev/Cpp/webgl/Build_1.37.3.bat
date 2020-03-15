@echo off

call emsdk activate sdk-fastcomp-1.37.40-64bit

rem emscripten configuration
call emcmake cmake -G "MinGW Makefiles"

rem build
mingw32-make

copy libEffekseerUnity.bc ..\..\Plugin\Assets\Effekseer\Plugins\WebGL\
