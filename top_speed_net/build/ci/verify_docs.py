#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import sys
import zipfile
from pathlib import Path


REQUIRED_DOCS = (
    "game-guide.html",
    "track-creation-guide.html",
    "vehicle-physics-and-creation-guide.html",
)


def normalize(path: str) -> str:
    value = path.replace("\\", "/")
    while value.startswith("./"):
        value = value[2:]
    while value.startswith("/"):
        value = value[1:]
    return value


def verify_archive(archive_path: Path, prefix: str) -> int:
    if not archive_path.is_file():
        print(f"Missing archive: {archive_path}", file=sys.stderr)
        return 1

    expected = {normalize(f"{prefix}/{name}") for name in REQUIRED_DOCS}
    with zipfile.ZipFile(archive_path, "r") as archive:
        entries = {normalize(name) for name in archive.namelist()}

    missing = sorted(item for item in expected if item not in entries)
    if missing:
        print(f"Documentation verification failed for {archive_path}:", file=sys.stderr)
        for item in missing:
            print(f"  - Missing {item}", file=sys.stderr)
        return 1

    print(f"Documentation files verified in {archive_path}.")
    return 0


def verify_directory(directory_path: Path, prefix: str) -> int:
    if not directory_path.is_dir():
        print(f"Missing directory: {directory_path}", file=sys.stderr)
        return 1

    missing: list[str] = []
    for name in REQUIRED_DOCS:
        relative = Path(prefix) / name if prefix else Path(name)
        full_path = directory_path / relative
        if not full_path.is_file():
            missing.append(str(relative).replace("\\", "/"))

    if missing:
        print(f"Documentation verification failed for {directory_path}:", file=sys.stderr)
        for item in missing:
            print(f"  - Missing {item}", file=sys.stderr)
        return 1

    print(f"Documentation files verified in {directory_path}.")
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Verify required docs are packaged correctly.")
    parser.add_argument("--archive", type=Path, help="Zip/APK file to inspect.")
    parser.add_argument("--directory", type=Path, help="Directory to inspect.")
    parser.add_argument("--prefix", default="docs", help="Path prefix inside archive/directory.")
    args = parser.parse_args()

    if bool(args.archive) == bool(args.directory):
        parser.error("Specify exactly one of --archive or --directory.")

    args.prefix = normalize(args.prefix)
    return args


def main() -> int:
    args = parse_args()
    if args.archive:
        return verify_archive(args.archive, args.prefix)
    return verify_directory(args.directory, args.prefix)


if __name__ == "__main__":
    sys.exit(main())
