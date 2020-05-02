#!/usr/bin/env python3

import sys
import os
import io
import argparse
import os.path
import tarfile
import yaml
import glob

parser = argparse.ArgumentParser(description='Create unitypackage without Unity')

parser.add_argument('-r', '--recursive', action='store_true')
parser.add_argument('targets', nargs='*', help='Target directory or file to pack')
parser.add_argument('-o', '--output', required=True, help='Output unitypackage path')

args = parser.parse_args()

print('Targets:', args.targets)
print('Output unitypackage:', args.output)
print('Is recursive', args.recursive)

for target in args.targets:
    if not os.path.exists(target):
        print("Target doesn't exist: " + target)
        sys.exit(1)

def filter_tarinfo(tarinfo):
    tarinfo.uid = tarinfo.gid = 0
    tarinfo.uname = tarinfo.gname = "root"
    return tarinfo

def add_file(tar, metapath):
    filepath = metapath[0:-5]
    print(filepath)
    with open(metapath, 'r') as f:
        try:
            guid = yaml.safe_load(f)['guid']
        except yaml.YAMLError as exc:
            print(exc)
            return

    # dir
    tarinfo = tarfile.TarInfo(guid)
    tarinfo.type = tarfile.DIRTYPE
    tar.addfile(tarinfo=tarinfo)

    if os.path.isfile(filepath):
        tar.add(filepath, arcname=os.path.join(guid, 'asset'), filter=filter_tarinfo)
    tar.add(metapath, arcname=os.path.join(guid, 'asset.meta'), filter=filter_tarinfo)
    # path: {guid}/pathname
    # text: path of asset
    tarinfo = tarfile.TarInfo(os.path.join(guid, 'pathname'))
    tarinfo.size= len(filepath)
    tar.addfile(tarinfo=tarinfo, fileobj=io.BytesIO(filepath.encode('utf8')))

with tarfile.open(args.output, 'w:gz') as tar:
    for target in args.targets:
        add_file(tar, target + '.meta')
        if args.recursive:
            for meta in glob.glob(os.path.join(target, '*.meta')):
                add_file(tar, meta)
            for meta in glob.glob(os.path.join(target, '**/*.meta'), recursive=True):
                add_file(tar, meta)
