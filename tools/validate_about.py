#!/usr/bin/env python3
"""Validate RimWorld mod metadata without requiring the game installation."""

from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ABOUT_FILES = (
    Path("About/About.xml"),
    Path("tools/Ports1deProbe/About/About.xml"),
)

package_ids: set[str] = set()
for about_path in ABOUT_FILES:
    if not about_path.is_file():
        print(f"ERROR: {about_path} is missing.")
        sys.exit(1)

    try:
        root = ET.parse(about_path).getroot()
    except ET.ParseError as exc:
        print(f"ERROR: {about_path} is not valid XML: {exc}")
        sys.exit(1)

    if root.tag != "ModMetaData":
        print(f"ERROR: root element in {about_path} must be ModMetaData.")
        sys.exit(1)

    text_fields = ("name", "author", "packageId", "description")
    missing = [
        tag for tag in text_fields
        if not (root.findtext(tag) or "").strip()
    ]
    if root.find("supportedVersions") is None:
        missing.append("supportedVersions")

    if missing:
        print(
            f"ERROR: {about_path} is missing required metadata: "
            + ", ".join(missing)
        )
        sys.exit(1)

    versions = [
        (node.text or "").strip()
        for node in root.findall("supportedVersions/li")
    ]
    if "1.7" not in versions:
        print(f"ERROR: {about_path} must declare supported version 1.7.")
        sys.exit(1)

    package_id = (root.findtext("packageId") or "").strip().lower()
    if package_id in package_ids:
        print(f"ERROR: duplicate packageId: {package_id}")
        sys.exit(1)
    package_ids.add(package_id)

    print(f"{about_path} is valid.")

print("All mod metadata files are valid.")
