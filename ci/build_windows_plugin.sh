SCRIPT_DIR=$(cd $(dirname $0); pwd)
ssh effekseer@192.168.56.102 'cd /mnt/c; rm -rf workdir;mkdir workdir'

scp -r `pwd` effekseer@192.168.56.102:/mnt/c/workdir/EffekseerForUnity
scp  $SCRIPT_DIR/build.bat effekseer@192.168.56.102:/mnt/c/workdir/build.bat

ssh effekseer@192.168.56.102 <<EOC
cd /mnt/c/workdir
rd /s /q Effekseer
git clone --recursive https://github.com/effekseer/Effekseer.git
/mnt/c/Windows/System32/cmd.exe /c 'C:\workdir\EffekseerForUnity\Dev\Cpp\windows\Build.bat' | nkf -w
EOC

scp effekseer@192.168.56.102:/mnt/c/workdir/EffekseerForUnity/Dev/Cpp/build_x86/Release/EffekseerUnity.dll `pwd`/Dev/Plugin/Assets/Effekseer/Plugins/x86
scp effekseer@192.168.56.102:/mnt/c/workdir/EffekseerForUnity/Dev/Cpp/build_x64/Release/EffekseerUnity.dll `pwd`/Dev//Plugin/Assets/Effekseer/Plugins/x86_64