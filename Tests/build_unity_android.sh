cd `dirname $0`

./../Dev/Plugin/BuildPackage.sh
cd `dirname $0`
UNITY_PATH=/Applications/2018.4.12f1/Unity.app/Contents/MacOS/Unity  
PROJECT_PATH=`pwd`/TestProject

$UNITY_PATH -projectPath $PROJECT_PATH -quit -batchmode -logFile  -executeMethod BuildClass.Build

