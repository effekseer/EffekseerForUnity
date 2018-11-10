@echo off

rem Enable Visual Studio 2015 environment
call "%VS140COMNTOOLS% \VsDevCmd.bat"

msbuild EffekseerUnity.sln /t:rebuild /property:Configuration=Release /p:platform=x86
msbuild EffekseerUnity.sln /t:rebuild /property:Configuration=Release /p:platform=x64

copy Win32\Release\EffekseerUnity.dll ..\..\Plugin\Assets\Effekseer\Plugins\x86\
copy x64\Release\EffekseerUnity.dll ..\..\Plugin\Assets\Effekseer\Plugins\x86_64\
