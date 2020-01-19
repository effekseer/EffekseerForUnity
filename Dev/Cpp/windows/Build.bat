cd /d %~dp0

mkdir ..\build_x86

cd /d ..\build_x86

cmake -A Win32 ../
cmake --build . --config Release

copy Release\EffekseerUnity.dll ..\..\Plugin\Assets\Effekseer\Plugins\x86\

cd /d %~dp0

mkdir ..\build_x64

cd /d ..\build_x64

cmake -A x64 ../
cmake --build . --config Release

copy Release\EffekseerUnity.dll ..\..\Plugin\Assets\Effekseer\Plugins\x86_64\



pause