@echo off

rem Enable Visual Studio 2013 environment
call "%VS120COMNTOOLS% \VsDevCmd.bat"

msbuild EffekseerUnity.sln /t:rebuild /property:Configuration=Release /p:platform=x86
msbuild EffekseerUnity.sln /t:rebuild /property:Configuration=Release /p:platform=x64

copy Win32\Release\EffekseerUnity.dll ..\..\Plugin\Assets\Plugins\x86\
copy x64\Release\EffekseerUnity.dll ..\..\Plugin\Assets\Plugins\x64\
