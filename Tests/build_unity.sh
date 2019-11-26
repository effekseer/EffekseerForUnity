cd `dirname $0`

./../Dev/Plugin/BuildPackage.sh
cd `dirname $0`
UNITY_PATH=/Applications/2018.4.12f1/Unity.app/Contents/MacOS/Unity  
PROJECT_PATH=`pwd`/TestProject

git clone https://github.com/effekseer/TestData.git TestProject/Resources/TestData
python3 test_tools.py

$UNITY_PATH -projectPath $PROJECT_PATH -quit -batchmode -logFile log -importPackage `pwd`/../Effekseer.unitypackage
$UNITY_PATH -projectPath $PROJECT_PATH -quit -batchmode -logFile log -executeMethod BuildClass.Build
