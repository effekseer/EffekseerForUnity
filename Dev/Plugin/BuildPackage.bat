@echo off

set UNITY="C:\Program Files\Unity\Editor\Unity.exe"
set PROJECT_PATH=%~dp0
set EXPORT_PACKAGES=Assets
set PACKAGE_NAME=Effekseer.unitypackage
set LOG_FILE=log.txt

%UNITY% ^
 -exportPackage %EXPORT_PACKAGES% %PACKAGE_NAME% ^
 -projectPath %PROJECT_PATH% ^
 -batchmode ^
 -nographics ^
 -logfile %LOG_FILE% ^
 -quit

copy %PACKAGE_NAME% ..\..\