# SavorPinoy — Group IT28

**Capstone:** SAVORPINOY: A 2D INTERACTIVE CULINARY SIMULATOR FOR PRESERVING CALABARZON TRADITIONAL FOOD HERITAGE

Unity capstone prototype for a 2D Philippine culinary heritage game. The Unity project is **`SavorPinoyPrototype/`**, not this repository root.

## Prototype

The prototype features Main Menu navigation and a Batangas cooking stage for **Adobo sa Dilaw**. Players select traditional ingredients such as luyang dilaw, garlic, and meat, prepare the dish, and earn a culinary score. Performance records are saved to Firebase Realtime Database and high scores can be loaded for the leaderboard.

The Unity client tracks choices in memory, computes a score, and writes a JSON record under `/player_scores`. When requested, it retrieves records from that node and displays the leaderboard. Records include `player_name`, `score`, `dish_name`, `level`, `remarks`, and `created_at`.

**Group members:** Markus Johann L. Catacutan, Alano (Jairus Reive), and Gaile Dichoso.

**Tools:** Unity Editor, C#, Firebase Realtime Database, Figma, and GitHub Desktop.

## First checkout

Install Git, [Git LFS](https://git-lfs.com/), Unity Hub, and **Unity 6000.6.3f1**. For Android, add that editor's Android Build Support, SDK/NDK Tools and OpenJDK modules. Python 3.10+ runs the repository preflight without third-party packages.

```sh
git lfs install
git clone https://github.com/MarkusLopezCatacutan25/GroupIT28_DatabaseConnectivity.git
cd GroupIT28_DatabaseConnectivity
git lfs pull
python tools/dev01_check.py --require-tracked
git lfs fsck
```

In Unity Hub, choose **Add project from disk** and select `SavorPinoyPrototype`. Open it with the pinned editor. Allow package/asset import to finish, then run **SavorPinoy → DEV-01 → Validate Project Setup**. Open `Assets/Scenes/MainMenu.unity` and press Play. The scene flow is MainMenu → RegionalTriviaHub → CookingStage. Kitchen guided practice is intended for the first playthrough.

The project targets Android SDK 26 minimum and ARM64. Unity package versions and their resolution are committed in `Packages/manifest.json` and `Packages/packages-lock.json`. Source scenes use Force Text serialization and Visible Meta Files.

## Working with assets

- Put game sprites in `SavorPinoyPrototype/Assets/Sprites/`. First imports default to Single Sprite, 100 PPU, bilinear filtering, Clamp, alpha transparency, no mipmaps, maximum 2048 and uncompressed textures. Designers can override settings afterward; reimport preserves overrides and GUIDs.
- Move and rename assets in Unity's Project window so their `.meta` files follow them. Commit each asset and its metadata together. Existing sliced sprites and borders are preserved.
- Images, audio, fonts and native SDK binaries use Git LFS. Unity scenes, prefabs, `.asset` files and `.meta` files remain text in normal Git. Commit `.gitattributes` whenever adding a binary type.
- Library, Temp, Logs, UserSettings, builds, generated resolver output, generated Firebase desktop configuration and exported `.unitypackage` archives are excluded. Original imported Firebase SDK source binaries remain included; Android resolver output can be regenerated with **Assets → External Dependency Manager → Android Resolver → Force Resolve**.

## Before committing

```sh
python -m unittest discover -s tools/tests -v
python tools/dev01_check.py
git add .gitattributes .gitignore .editorconfig README.md .github tools SavorPinoyPrototype
python tools/dev01_check.py --require-tracked
git diff --cached --stat
git lfs status
git lfs fsck
```

Inspect the staged file list before committing. Never add signing keys, service-account private keys or local environment files. Assets/google-services.json is the canonical Firebase client configuration; Unity regenerates the ignored desktop configuration from it. The client configuration is app configuration, not a server credential; database configuration/security and online backend acceptance belong to DEV-05.

**DEV-01 Repository Check** runs automatically on GitHub pushes and pull requests. It validates repository structure and assets; it does not build an APK or claim a Unity gameplay/pilot test. First imports may need network access to Unity Package Manager and Android dependencies.

## Scope and references

This is a classroom prototype. Advanced mechanics such as knife-slicing physics, a jeepney travel map hub, and daily nutritional habit tracking are future iterations. The instructor GitHub account `gracheleliza` was added as a collaborator.

References: [Firebase Unity SDK Documentation](https://firebase.google.com/docs/unity/setup) and the UPHSL 1stBIT4120L Technical Guide for Activity 5.

See [DEV-01 setup and handoff](SavorPinoyPrototype/Docs/DEV-01-Repo-Project-Setup.md) for acceptance evidence, the Unity batch command, and the GitHub checklist.
