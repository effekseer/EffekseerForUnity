cd `dirname $0`

UNITY=/Applications/2018.4.12f1/Unity.app/Contents/MacOS/Unity 
PROJECT_PATH=`pwd`
EXPORT_PACKAGES=Assets
PACKAGE_NAME=Effekseer.unitypackage
LOG_FILE=log.txt

$UNITY \
 -exportPackage $EXPORT_PACKAGES $PACKAGE_NAME \
 -projectPath $PROJECT_PATH  \
 -batchmode \
 -nographics \
 -logfile $LOG_FILE \
 -quit

cp $PACKAGE_NAME ../../