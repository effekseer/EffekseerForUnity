call "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\Common7\Tools\VsDevCmd.bat"
cd C:\workdir\EffekseerForUnity\Dev\Cpp\windows


msbuild EffekseerUnity.sln /t:rebuild /property:Configuration=Release /p:platform=x86
msbuild EffekseerUnity.sln /t:rebuild /property:Configuration=Release /p:platform=x64