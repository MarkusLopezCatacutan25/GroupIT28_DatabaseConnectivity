# SavorPinoy — Group 28

> 3 November update: two-province trivia and preparation changes are described in [the November release notes](Docs/DEV-03/November03/README.md). Quezon live saves require publication of the revised rules; October live evidence is historical.


**SAVORPINOY: A 2D Interactive Culinary Simulator for Preserving CALABARZON Traditional Food Heritage**

Members: Markus Johann L. Catacutan; Jairus Reive B. Alano; Gaile Dichoso. Target learners: UPHSL Junior High School TLE and first-year CIHM culinary students.

The Unity project is **SavorPinoyPrototype/**. Open it with **Unity 6000.6.3f1**. The active gameplay scene is `Assets/Scenes/CookingStage.unity`; BatangasStage is deprecated. Targets are Android and PC; Editor verification does not establish Android build/device verification.

## Current vertical slice

MainMenu → RegionalTriviaHub → Batangas / Adobo sa Dilaw → cooking rules → Firebase Anonymous Authentication UID → atomic Realtime Database write → matching read-back → UI confirmation → Continue.

The scene has separate domain, UI, identity, persistence and query components. A nickname is display text only. Continue requires correct cooking **and** a confirmed write/read round trip under the same current UID. Local queueing and pending writes cannot complete the stage. My attempts and Top scores expose Loading, Empty and retryable Error states.

The supplied Singapore database URL is configured in `Assets/Settings/FirebaseSettings.asset`. The owner enabled Anonymous sign-in and published the new rules. **All 25 live Unity SDK integration checks passed against the Singapore instance on 27 October 2026, including the visible sign-in and scene-entry guards.** Local emulator results remain separately labeled. See [vertical-slice documentation](Docs/VerticalSlice/README.md), [test scope](Docs/VerticalSlice/TESTING.md), [exact contract](Docs/VerticalSlice/DATA-CONTRACT.md), and [live verification and presentation checklist](Docs/VerticalSlice/LIVE-ACCEPTANCE.md).

## Clone and play

```sh
git lfs install
git clone https://github.com/MarkusLopezCatacutan25/GroupIT28_DatabaseConnectivity.git
cd GroupIT28_DatabaseConnectivity
git lfs pull
python tools/dev01_check.py --require-tracked
git lfs fsck
```

1. Unity Hub → Add project from disk → select `SavorPinoyPrototype/`. Wait for import. Auth and Database from the same Firebase Unity SDK **13.17.0** are already imported; do not mix versions.
2. Complete the Firebase owner steps in LIVE-ACCEPTANCE.md. Existing project: **savorpinoy-database-daf6f**. Never add service-account keys or login tokens to Git.
3. Open `Assets/Scenes/MainMenu.unity`, press Play, optionally enter a chef name, then choose **Continue as guest**. After successful sign-in, select CALABARZON/Batangas, resolve trivia, then Proceed to Kitchen. A direct cooking scene launch without a resolved session returns to the hub.
4. Your chef display name is carried forward from sign-in. Select Guided practice (stopwatch) or Timed challenge. Gather garlic, turmeric and chicken; choose Knife and follow the preparation guides. Control the flame, add ingredients in order, simmer vinegar without stirring, turn off heat and plate.
5. On results, Save attempt and wait for **Saved to Firebase and read back successfully**. My attempts reloads owned data; Top scores reloads shared challenge attempts. Only a confirmed correct attempt unlocks Continue.

Firebase application configuration is under Assets; the backend URL is explicitly set in the settings asset. For Android, check the registered application identifier and resolve External Dependency Manager dependencies before building.

## Repository and evidence

Keep Assets, Packages, ProjectSettings and `.meta` files in Git. Library, Temp, logs, builds and user settings are ignored. Large art/audio/fonts/native binaries use Git LFS; scenes and metadata remain text. See [DEV-01 setup](SavorPinoyPrototype/Docs/DEV-01-Repo-Project-Setup.md).

`Docs/Activity5/` and `tools/activity5/` are historical evidence for the superseded `/player_scores` contract. Use `tools/vertical-slice/` for the current `/users/<uid>/attempts` and `/leaderboard` contract. Archived legacy runtime source is under `Docs/LegacyActivity5/`, outside Assets. Existing remote records and local legacy files are preserved; there is no automatic reassignment or destructive migration.

Rules enforce ownership and schema, not proof of honest gameplay. Scores and timestamps are client supplied. Anonymous accounts are installation-specific, not student identity. Classroom usability, heritage content validation and learning outcomes require separate review.

References: institutional *Building a Capstone Prototype the Right Way: One Vertical Slice* guideline; [Firebase Unity setup](https://firebase.google.com/docs/unity/setup), [Anonymous Authentication](https://firebase.google.com/docs/auth/unity/anonymous-auth), [RTDB saving](https://firebase.google.com/docs/database/unity/save-data), [retrieval](https://firebase.google.com/docs/database/unity/retrieve-data), [security rules](https://firebase.google.com/docs/database/security). AI assistance was used; group members must review and be able to explain the code.
