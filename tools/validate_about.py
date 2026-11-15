#!/usr/bin/env python3
"""Validate the basic RimWorld mod metadata without needing the game installed."""

from pathlib import Path
import sys
import xml.etree.ElementTree as ET

about_path = Path("About/About.xml")
if not about_path.is_file():
    print("ERROR: About/About.xml is missing.")
    sys.exit(1)

try:
    root = ET.parse(about_path).getroot()
except ET.ParseError as exc:
    print(f"ERROR: About.xml is not valid XML: {exc}")
    sys.exit(1)

required = ("name", "author", "packageId", "supportedVersions", "description")
missing = [tag for tag in required if root.find(tag) is None or not (root.findtext(tag) or "").strip()]
if root.tag != "ModMetaData":
    print("ERROR: root element must be ModMetaData.")
    sys.exit(1)

if missing:
    print("ERROR: missing required metadata: " + ", ".join(missing))
    sys.exit(1)

versions = [node.text for node in root.findall("supportedVersions/li")]
if "1.7" not in versions:
    print("ERROR: supportedVersions must include 1.7.")
    sys.exit(1)

print("About.xml is valid.")
