"""DEV-01 source/LFS preflight; Python 3.10+, standard library, no Unity license needed."""
from pathlib import Path, PurePosixPath
import argparse
import json
import re
import subprocess
import sys

PROJECT = "SavorPinoyPrototype"
SCENES = ["MainMenu", "RegionalTriviaHub", "CookingStage"]
OPAQUE = {".androidlib", ".bundle", ".framework", ".xcframework", ".plugin"}
BINARY = {".png", ".jpg", ".jpeg", ".psd", ".psb", ".tga", ".tif", ".tiff", ".wav", ".mp3", ".ogg", ".mp4", ".fbx", ".blend", ".ttf", ".otf", ".dll", ".so", ".bundle", ".aar", ".srcaar", ".jar", ".exe", ".bytes", ".bin"}


def git(root, *args, data=None):
    result = subprocess.run(["git", "-C", str(root), *args], input=data, capture_output=True)
    if result.returncode:
        raise RuntimeError(result.stderr.decode(errors="replace").strip())
    return result.stdout


def listed(root, tracked=False):
    args = ["ls-files", "-z", "--cached"]
    if not tracked:
        args += ["--others", "--exclude-standard"]
    return sorted(set(p.decode("utf-8") for p in git(root, *args).split(b"\0") if p))


def opaque(relative):
    # Unity imports native plugin directories as a unit; internal files have no .meta.
    return any(PurePosixPath(part).suffix.lower() in OPAQUE for part in PurePosixPath(relative).parts[:-1])


def metadata_errors(root, asset_paths):
    errors, needed, guids = [], set(), {}
    for name in asset_paths:
        path = PurePosixPath(name)
        if path.suffix == ".meta" or opaque(name):
            continue
        needed.add(name + ".meta")
        for parent in path.parents:
            if parent.as_posix() == PROJECT + "/Assets":
                break
            if parent.as_posix().startswith(PROJECT + "/Assets/"):
                needed.add(parent.as_posix() + ".meta")
    available = set(asset_paths)
    for name in sorted(needed - available):
        errors.append("Missing or untracked metadata: " + name)
    for name in asset_paths:
        if not name.endswith(".meta") or opaque(name):
            continue
        path = root / name
        if not path.exists():
            errors.append("Missing working file: " + name)
            continue
        match = re.search(r"^guid: ([a-f0-9]{32})$", path.read_text(encoding="utf-8-sig"), re.M)
        if not match:
            errors.append("Invalid metadata GUID: " + name)
        elif match[1] in guids:
            errors.append("Duplicate GUID: " + name + " and " + guids[match[1]])
        else:
            guids[match[1]] = name
        if not (root / name[:-5]).exists():
            errors.append("Orphan metadata: " + name)
    return errors


def inspect(root, require_tracked=False):
    errors = []
    files = listed(root, require_tracked)
    available = set(files)
    base = root / PROJECT
    required = [".gitattributes", PROJECT + "/.gitignore", PROJECT + "/Packages/manifest.json", PROJECT + "/Packages/packages-lock.json", PROJECT + "/ProjectSettings/ProjectVersion.txt", PROJECT + "/Assets/Editor/SavorPinoyAssetPipeline.cs"]
    for name in required:
        if name not in available:
            errors.append("Missing or untracked required source: " + name)
    if errors:
        return errors, {"files": len(files)}
    if "m_EditorVersion: 6000.6.3f1" not in (base / "ProjectSettings/ProjectVersion.txt").read_text():
        errors.append("Unity version must be 6000.6.3f1.")
    settings = (base / "ProjectSettings/EditorSettings.asset").read_text()
    if not re.search(r"m_SerializationMode: 2\b", settings):
        errors.append("Unity serialization must be Force Text.")
    version_control = (base / "ProjectSettings/VersionControlSettings.asset").read_text()
    if "m_Mode: Visible Meta Files" not in version_control:
        errors.append("Visible Meta Files must be enabled.")
    build = (base / "ProjectSettings/EditorBuildSettings.asset").read_text()
    scenes = re.findall(r"- enabled: 1\s+path: (.+)", build)
    expected = ["Assets/Scenes/" + name + ".unity" for name in SCENES]
    if scenes != expected:
        errors.append("Enabled scenes must be MainMenu, RegionalTriviaHub, CookingStage in order.")
    for scene in expected:
        if PROJECT + "/" + scene not in available:
            errors.append("Missing or untracked build scene: " + scene)
    deps = json.loads((base / "Packages/manifest.json").read_text())["dependencies"]
    lock = json.loads((base / "Packages/packages-lock.json").read_text())["dependencies"]
    for key, version in deps.items():
        if key not in lock:
            errors.append("Dependency missing from package lock: " + key)
        elif re.match(r"^\d+\.\d+\.\d+", version) and lock[key].get("version") != version:
            errors.append("Manifest/lock version mismatch: " + key)
    for name in files:
        relative = PurePosixPath(name)
        parts = [part.lower() for part in relative.parts]
        if len(parts) > 1 and parts[0] == PROJECT.lower() and parts[1] in {"library", "temp", "obj", "logs", "usersettings", "build", "builds"}:
            errors.append("Generated cache/build file is included: " + name)
        if relative.suffix.lower() in {".keystore", ".jks", ".p12", ".pfx"} or relative.name.startswith(".env") and relative.name != ".env.example":
            errors.append("Local credential/signing file is included: " + name)
        path = root / name
        if not path.is_file():
            errors.append("Missing working file: " + name)
            continue
        if relative.suffix.lower() == ".json" and path.stat().st_size < 2_000_000:
            contents = path.read_text(encoding="utf-8-sig", errors="replace")
            if re.search(r'"type"\s*:\s*"service_account"|"private_key"\s*:', contents):
                errors.append("Server credential file must not be committed: " + name)
        if path.stat().st_size < 1024 and path.read_bytes().startswith(b"version https://git-lfs.github.com/spec/v1"):
            errors.append("Unhydrated LFS pointer; run git lfs pull: " + name)
    assets = [name for name in files if name.startswith(PROJECT + "/Assets/")]
    errors += metadata_errors(root, assets)
    binaries = [name for name in assets if PurePosixPath(name).suffix.lower() in BINARY]
    if binaries:
        data = b"".join(name.encode() + b"\0" for name in binaries)
        attrs = git(root, "check-attr", "-z", "--stdin", "filter", data=data).split(b"\0")
        for i in range(0, len(attrs) - 1, 3):
            if attrs[i+2] != b"lfs":
                errors.append("Binary asset is not assigned to LFS: " + attrs[i].decode())
    probes = [PROJECT + "/Library/probe", PROJECT + "/Temp/probe", PROJECT + "/Logs/probe", PROJECT + "/UserSettings/probe", PROJECT + "/Assets/GeneratedLocalRepo/probe"]
    ignored = subprocess.run(["git", "-C", str(root), "check-ignore", "-z", "--stdin"], input=b"".join(name.encode()+b"\0" for name in probes), capture_output=True)
    if {name.decode() for name in ignored.stdout.split(b"\0") if name} != set(probes):
        errors.append("Nested Unity caches/generated resolver files are not all ignored.")
    return errors, {"files": len(files), "asset_files": len(assets), "lfs_assets": len(binaries), "scenes": len(scenes), "dependencies": len(deps)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--require-tracked", action="store_true", help="Inspect the Git index instead of including untracked files.")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    try:
        errors, counts = inspect(args.root.resolve(), args.require_tracked)
    except (OSError, RuntimeError, KeyError, ValueError) as exc:
        errors, counts = [str(exc)], {}
    report = {"passed": not errors, "counts": counts, "errors": errors}
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))
    return bool(errors)


if __name__ == "__main__":
    sys.exit(main())
