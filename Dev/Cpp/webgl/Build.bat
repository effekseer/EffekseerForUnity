@echo off

rem Enable Visual Studio 2015 environment
call "%VS140COMNTOOLS% \VsDevCmd.bat"

rem emscripten configuration
call emcmake cmake -G "MinGW Makefiles"

rem build
nmake

copy libEffekseerUnity.bc ..\..\Plugin\Assets\Plugins\WebGL\
