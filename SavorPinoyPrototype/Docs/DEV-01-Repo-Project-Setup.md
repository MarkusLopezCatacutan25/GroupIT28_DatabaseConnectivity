# DEV-01: Repo & Project Setup

Owner: Catacutan, Markus. Scope: game engine project setup, Git LFS and the asset import pipeline. Prepared 17 October 2026 (Asia/Singapore). DEV-02 gameplay, DEV-03 new art/levels, DEV-04 audio/VFX, DEV-05 online backend, APK delivery and pilot evaluation remain separate milestones.

## Baseline and changes

The repository initially contained only README.md and a Unity ignore template. The working Unity project was an untracked nested folder, so root-anchored cache exclusions did not cover it. No .gitattributes or LFS-tracked assets existed. The editor was already pinned to 6000.6.3f1, scene order and Android settings were present, and asset GUIDs had no duplicates.

The setup adds project-scoped ignore rules, shared binary LFS policy, text serialization attributes, editor formatting defaults, the complete existing Unity source baseline, automated first-import sprite defaults, in-editor setup validation, a dependency-free repository validator and its regression cases, and a GitHub workflow. Existing assets, scene GUIDs, gameplay and package versions are retained. Native Android plugin directories are treated as opaque importer units: Unity does not require .meta files for their internal AndroidManifest/res files.

## Acceptance checks

| Requirement | Evidence |
| --- | --- |
| Correct engine project | ProjectVersion.txt pins Unity 6000.6.3f1; open the nested SavorPinoyPrototype folder. |
| Reproducible dependencies | Both package manifest and package lock are versioned; preflight checks direct dependency versions. |
| Unity collaboration settings | Force Text and Visible Meta Files; required scenes ordered MainMenu, RegionalTriviaHub, CookingStage. |
| Android project foundation | Minimum SDK 26 and ARM64 validated in the Editor. This is not an APK/device test. |
| Git hygiene | Library/Temp/Logs/UserSettings/builds/generated local resolver files are excluded at the nested project boundary. |
| Git LFS | Binary policy includes art, fonts, audio and Firebase native binaries, including files above normal Git file limits. Text assets and metadata stay in Git. |
| Asset import pipeline | New Assets/Sprites imports receive defaults. The batch probe checks a first import, changes filtering, reimports and verifies that the new authored value and GUID persist. Probe deletes only its own unique test asset. |
| Asset integrity | Required .meta files, valid/unique GUIDs, orphan metadata, hydrated LFS assets and lock consistency checked by tools/dev01_check.py. |
| Team onboarding | Root README documents checkout, Unity Hub selection, imports, scene startup and review before commit. |
| Automation | DEV-01 Repository Check workflow runs validator tests, tracked-source checks and LFS object integrity. Cloud execution requires a push. |

## Local verification

Run from the repository root:

```sh
python -m unittest discover -s tools/tests -v
python tools/dev01_check.py --require-tracked
git lfs fsck
```

For a batch Unity import/setup check on a checkout not open in the Editor, use PowerShell:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' `
  -batchmode -quit -projectPath '<absolute path to SavorPinoyPrototype>' `
  -executeMethod SavorPinoyProjectSetup.ValidateBatch `
  -logFile '<absolute path to dev01-unity.log>'
```

Passing output includes DEV-01 PASS lines for settings, opening three source scenes without missing scripts, and first-import/reimport behavior. AssetDatabase automatically generates files under ignored cache directories. Opening a clone may require initial package resolution; LFS pointers must be hydrated before Unity starts. GitHub's static workflow does not replace this editor verification.

## What Markus should check on GitHub

The local milestone must be pushed before the website can show it. In GitHub Desktop, select GroupIT28_DatabaseConnectivity and confirm the local DEV-01 commit, then use Push origin. If other commits are pending, review them too before pushing. No history rewrite or force push is needed.

1. On the repository's Code tab, select the branch containing the DEV-01 commit. Confirm SavorPinoyPrototype/Assets, Packages and ProjectSettings are present, along with .gitattributes, tools and the root README.
2. Open a PNG or Firebase native binary. GitHub should identify it as stored with Git LFS. Library, Temp, Logs and UserSettings should not be in the source tree. The exported Mid-Act7.unitypackage archive is intentionally excluded.
3. Open Actions → DEV-01 Repository Check. The run for that commit must pass. An LFS download/quota error is a remote storage issue, not a successful milestone check; copy the failed step's message for diagnosis.
4. Have one teammate clone into a new folder, run git lfs pull and the README checks, then open MainMenu with Unity 6000.6.3f1. Confirm Play reaches recipe selection without missing-asset or missing-script errors.
5. In the capstone tracker, set DEV-01 to Done after the pushed commit, green workflow and teammate checkout have been confirmed. The tracker has not been edited by this task.

## References

- [Git LFS official setup](https://git-lfs.com/)
- [Git LFS repository and tracking workflow](https://github.com/git-lfs/git-lfs)
- [Unity texture import callback](https://docs.unity3d.com/6000.0/ScriptReference/AssetPostprocessor.OnPreprocessTexture.html)
- [GitHub checkout action](https://github.com/actions/checkout)
- [GitHub Python setup action](https://github.com/actions/setup-python)

Local verification results are recorded below after the release checks. Remote upload, hosted CI and teammate-device results are not inferred from local tests.
