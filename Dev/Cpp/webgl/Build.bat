@echo off

rem Enable Visual Studio 2013 environment
call "%VS120COMNTOOLS% \VsDevCmd.bat"

rem emscripten configuration
call emcmake cmake

rem build
nmake

copy libEffekseerUnity.bc ..\..\Plugin\Assets\Plugins\WebGL\
