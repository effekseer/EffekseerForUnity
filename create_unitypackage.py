#!/usr/bin/env python3

import sys
import os
import io
import argparse
import os.path
import posixpath
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
    sourcepath = metapath[0:-5]
    pathname = sourcepath.replace('\\', '/')
    print(pathname)
    with open(metapath, 'r') as f:
        try:
            guid = yaml.safe_load(f)['guid']
        except yaml.YAMLError as exc:
            print(exc)
            return

    # dir
    tarinfo = tarfile.TarInfo(guid)
    tarinfo.type = tarfile.DIRTYPE
    tarinfo.mode = 0o755
    filter_tarinfo(tarinfo)
    tar.addfile(tarinfo=tarinfo)

    if os.path.isfile(sourcepath):
        tar.add(sourcepath, arcname=posixpath.join(guid, 'asset'), filter=filter_tarinfo)
    tar.add(metapath, arcname=posixpath.join(guid, 'asset.meta'), filter=filter_tarinfo)
    # path: {guid}/pathname
    # text: path of asset
    pathname_bytes = pathname.encode('utf-8')
    tarinfo = tarfile.TarInfo(posixpath.join(guid, 'pathname'))
    tarinfo.mode = 0o644
    filter_tarinfo(tarinfo)
    tarinfo.size = len(pathname_bytes)
    tar.addfile(tarinfo=tarinfo, fileobj=io.BytesIO(pathname_bytes))

with tarfile.open(args.output, 'w:gz') as tar:
    for target in args.targets:
        metapaths = [target + '.meta']
        if args.recursive:
            metapaths.extend(glob.glob(os.path.join(target, '**/*.meta'), recursive=True))

        seen = set()
        for metapath in metapaths:
            if metapath in seen:
                continue
            seen.add(metapath)
            add_file(tar, metapath)
