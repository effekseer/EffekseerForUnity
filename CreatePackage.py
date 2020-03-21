import shutil
import os

version = '150'

package_name = 'EffekseerForUnity{}/'.format(version)
if os.path.exists(package_name):
    shutil.rmtree(package_name)
os.makedirs(package_name, exist_ok=False)

shutil.copy('Effekseer.unitypackage', package_name)
shutil.copy('Help.html', package_name)
shutil.copy('readme.txt', package_name)
shutil.copy('LICENSE', package_name)
