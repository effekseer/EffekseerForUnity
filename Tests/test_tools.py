import glob

files =[]
for file in glob.glob('TestProject/Resources/TestData/**/*.efk', recursive=True) :
    files.append(file.replace('\\', '/').replace('TestProject/Resources', ''))
with open("TestProject/Resources/TestData/test-list.csv", "w") as f:
    f.write(','.join(files))