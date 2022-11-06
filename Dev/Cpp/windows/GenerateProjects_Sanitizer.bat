echo set current directory
cd /d %~dp0

mkdir ..\..\build_sanitizer

cd /d ..\..\build_sanitizer

cmake ../ -D BUILD_EXAMPLES=OFF -D SANITIZE_ENABLED=ON

pause