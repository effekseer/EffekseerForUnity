echo set current directory
cd /d %~dp0

mkdir ..\..\build

cd /d ..\..\build

cmake ../ -D BUILD_EXAMPLES=OFF

pause