import re
import codecs
import glob


class CreateHeader:
    def __init__(self):
        self.lines = []

    def readLines(self, path):
        f = codecs.open(path, 'r', 'utf-8_sig')
        line = f.readline()
        while line:
            self.lines.append(line)
            line = f.readline()
        f.close()

    def output(self, path):
        self.lines = [l.replace('\r\n','\n').replace('\n','\r\n') for l in self.lines]
        f = codecs.open(path, 'w', 'utf-8_sig')
        for line in self.lines:
            f.write(line)
        f.close()


effekseerCore = CreateHeader()

files = []
files += glob.glob('../Effekseer/Dev/Editor/EffekseerCore/*.cs')
files += glob.glob('../Effekseer/Dev/Editor/EffekseerCore/Binary/*.cs')
files += glob.glob('../Effekseer/Dev/Editor/EffekseerCore/Command/*.cs')
files += glob.glob('../Effekseer/Dev/Editor/EffekseerCore/Data/*.cs')
files += glob.glob('../Effekseer/Dev/Editor/EffekseerCore/Data/Value/*.cs')
files += glob.glob('../Effekseer/Dev/Editor/EffekseerCore/Utl/*.cs')
files += glob.glob('../Effekseer/Dev/Editor/EffekseerCore/InternalScript/*.cs')

for file in files:
    effekseerCore.readLines(file)

effekseerCore.lines = [l for l in effekseerCore.lines if not ('using ' in l)]
effekseerCore.lines = [l.replace(
    'namespace Effekseer', 'namespace EffekseerTool') for l in effekseerCore.lines]

using_lines = []
using_lines += ['using System;\n']
using_lines += ['using System.Linq;\n']
using_lines += ['using System.Collections;\n']
using_lines += ['using System.Collections.Generic;\n']
using_lines += ['using System.Threading;\n']
using_lines += ['using System.Xml;\n']
using_lines += ['using System.Runtime.InteropServices;\n']
using_lines += ['using System.Reflection;\n']
using_lines += ['using System.Text;\n']
using_lines += ['using System.Resources;\n']
using_lines += ['using System.Globalization;\n']
using_lines += ['using EffekseerTool.Data;\n']
using_lines += ['using EffekseerTool.Utl;\n']

effekseerCore.lines = using_lines + ['\n'] + effekseerCore.lines

effekseerCore.lines = ['#if UNITY_EDITOR\n'] + \
    effekseerCore.lines + ['#endif\n']

effekseerCore.output('EffekseerTool.cs')
