# SavorPinoy - Group 28 Database Connectivity Prototype

**Capstone:** SAVORPINOY: A 2D Interactive Culinary Simulator for Preserving CALABARZON Traditional Food Heritage

**Members:** Markus Johann L. Catacutan; Jairus Reive B. Alano; Gaile Dichoso.

**Repository:** https://github.com/MarkusLopezCatacutan25/GroupIT28_DatabaseConnectivity
**Visibility:** Public (verified through GitHub API during this revision). Instructor collaboration is not needed to view a public repository.

## Actual progress and evidence

The Unity project is `SavorPinoyPrototype/`, not this repository root. MainMenu -> RegionalTriviaHub -> CookingStage is implemented in source. Enter a chef nickname before starting a cooking mode, gather ingredients, prepare them with knife strokes, control heat, add ingredients in order, and plate. Timed challenge results can be queued for Firebase and a top-10 leaderboard can be refreshed. Guided practice saves a separate local history and does not enter the ranked board.

The source now includes Firebase Anonymous Authentication, owned score submissions, a durable local retry queue, and validation rules. Local Unity checks and Firebase emulator tests are documented in `Docs/Activity5/`. **These tests do not establish a successful save to the live Firebase project. Live deployment and the matching-record screenshot sequence are still pending.** Do not label the activity complete until that evidence is captured.

## Tools and exact versions

- Unity **6000.6.3f1**, C#, TextMeshPro/uGUI, Input System and Git LFS.
- Firebase Unity **13.17.0**, including `FirebaseAuth.unitypackage` and `FirebaseDatabase.unitypackage`; these SDK assemblies are already imported under `Assets/Firebase/`.
- Existing Firebase project: **savorpinoy-database-daf6f**. Use this project's Realtime Database; do not create a replacement database.
- Desktop Editor for reproducible first verification; Android SDK/NDK/OpenJDK support is needed only for Android builds.

## Clone and run

```sh
git lfs install
git clone https://github.com/MarkusLopezCatacutan25/GroupIT28_DatabaseConnectivity.git
cd GroupIT28_DatabaseConnectivity
git lfs pull
python tools/dev01_check.py --require-tracked
git lfs fsck
```

1. In Unity Hub, Add project from disk: select `SavorPinoyPrototype/`. Open with **6000.6.3f1** and wait for package/asset import.
2. Confirm Firebase Auth and Database DLLs are present. If missing, import Auth and Database from the same Firebase Unity SDK 13.17.0 release. Do not mix SDK versions. Restore `Assets/google-services.json` from the **existing Firebase project** if required. Never add a service-account key or private credential.
3. In Firebase Console, select **savorpinoy-database-daf6f**. Open Authentication -> Sign-in method and enable **Anonymous**. This creates a pseudonymous authenticated UID; a chef nickname alone is not authentication.
4. Open Realtime Database -> Data and copy the **exact database URL**. Open `Assets/Scenes/CookingStage.unity`, select the GameObject with `SavorPinoyCookingManager`, and paste it into **Database Url**. The saved configuration currently has no URL. The service can alternatively use the Firebase app configuration's DatabaseUrl if present.
5. Review and deploy the repository's `database.rules.json` to that database in Console -> Rules. Back up existing rules first; preserve unrelated paths if this database hosts other features. This revision does not publish rules automatically. Old records are preserved; new writes require `owner_uid`.
6. For Android, confirm the package identifier matches the registered Firebase Android application. Run Assets -> External Dependency Manager -> Android Resolver -> Force Resolve. Do not claim Android build verification from an Editor-only test.
7. Open **Assets/Scenes/MainMenu.unity**, press Play, choose CALABARZON/Batangas, resolve trivia, and proceed. Enter a nickname and choose **Timed challenge** for the database demonstration. Directly opening CookingStage without a resolved session returns to the hub.
8. Complete cooking, press **Save score**, and wait for **Saved successfully to Firebase: [record ID]**. A local-save or pending message is not cloud success. Press **Refresh scores** to retrieve the leaderboard.

## Saved data and identifiers

Path: `/player_scores/{score_id}`. `score_id` is the unique **node key**, `slice_` plus a 32-character run GUID; it is not another field inside the JSON payload. Retries reuse this key.

| Field | Actual type and constraints | Purpose |
|---|---|---|
| player_name | String, 1-24 nickname characters (letters/numbers, spaces, period, underscore, hyphen; must contain a letter/number) | Chef nickname, not student number |
| score | Integer 0-9375 | Final timed-challenge score |
| dish_name | String, `Adobo sa Dilaw` | Dish |
| level | String, `Batangas` | Stage |
| remarks | String, `batangas_slice_v1 \| 0-3 stars` | Rubric and result tier |
| created_at | String, ISO 8601 UTC from `DateTime.UtcNow.ToString("o")` | Example format `2026-10-18T12:00:00.0000000Z`; client clock, not server timestamp |
| owner_uid | String, equal to authenticated Firebase UID | Ownership metadata added to the original six gameplay fields |

Raw maximum: cutting 2500 + sequence 2500 + thermal 2500 = 7500. A correct trivia answer gives 1.25x, so the ranked maximum is **9375**. Penalties can reduce the score. There is no extra +500 in this prototype. Rules validate type/range; they cannot prove an untrusted client actually played the game.

`PrototypeScoreService` writes with `SetRawJsonValueAsync`, reconciles an existing identical record before retrying, and queries `OrderByChild("score").LimitToLast(10)`. Results are sorted highest first, with a key-based tie break, and show nickname, score, dish, record ID and timestamp in a scrollable list. This is refresh-based retrieval, not a realtime subscription. It ranks attempts rather than each player's personal best and has no cohort filter.

## Saving, failures and recovery

- Local pending results are stored before network submission in `Application.persistentDataPath/savorpinoy-prototype-outbox.json`, with backup/recovery candidates.
- Firebase sign-in must succeed before sending. Each queued record is bound to the signed-in UID before first transmission; another account cannot take over that result.
- UI distinguishes local save, signing in, saving/pending, acknowledged success and failure. A rejected result remains local; **Retry sync** retries the same record key after the issue is corrected.
- Do not clear application data or change accounts while records are pending. Anonymous accounts are installation-specific and do not establish a student's real identity.
- Guided practice has separate local history. Unfinished cooking is not restored after process termination.

## Required live evidence before resubmission

Use one distinctive nickname and preserve one record ID through all evidence:

1. Nickname input and ingredient interaction.
2. Completed result showing nickname, score and record ID before saving.
3. Acknowledged **Firebase** save message (not just local queue success).
4. Firebase Console Data showing the same node key and all seven fields.
5. Stop the game, start it again through MainMenu, complete another timed round to open results, press Refresh scores, and show the original record loaded from Firebase. Use a distinctive high-scoring test attempt so it is in the top 10; an old cache label is not proof of a fresh read.
6. Capture deployed ownership rules, relevant save/retrieve code, public repository, real commit history and this README.
7. Also test a denied request and a network interruption: retain the result, fix the cause, Retry sync, and confirm only one record exists.

## Repository and asset workflow

Keep `Assets/`, `Packages/`, `ProjectSettings/`, scripts and their `.meta` files in Git. Library, Temp, Logs, UserSettings and build output are ignored. Images, fonts, audio and native SDK binaries use Git LFS; scenes, prefabs and metadata remain text. Move assets inside Unity so GUIDs are preserved. Do not commit private keys, service accounts or signing credentials.

See `SavorPinoyPrototype/Docs/DEV-01-Repo-Project-Setup.md` for the asset import defaults and setup checks. DEV-01 completion does not imply live database acceptance.

## Known limitations and references

Live Anonymous sign-in, deployed rules and matching Unity/Firebase/reopened-leaderboard screenshots remain to be verified for the existing database. Legacy six-field records are not automatically migrated or removed. Ownership controls prevent impersonating another UID but do not stop an authenticated client fabricating an in-range score. Client timestamps are format checked, not trusted clock evidence. No cohort-based instructor assessment or validated learning-effectiveness claim is made.

References: UPHSL BIT4120L Activity 5 requirements and professor feedback; [Firebase Unity setup](https://firebase.google.com/docs/unity/setup); [Anonymous Authentication](https://firebase.google.com/docs/auth/unity/anonymous-auth); [Realtime Database rules](https://firebase.google.com/docs/database/security); [Saving](https://firebase.google.com/docs/database/unity/save-data); [Retrieval](https://firebase.google.com/docs/database/unity/retrieve-data). AI assistance was used for this revision; group members must review, understand and be able to explain the implementation.
