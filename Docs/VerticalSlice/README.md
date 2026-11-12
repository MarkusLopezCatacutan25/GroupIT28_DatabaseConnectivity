# SavorPinoy vertical slice — 27 October 2026

> 3 November update: two-province trivia and preparation changes are described in [the November release notes](../DEV-03/November03/README.md). Quezon live saves require publication of the revised rules; October live evidence is historical.


SAVORPINOY: A 2D Interactive Culinary Simulator for Preserving CALABARZON Traditional Food Heritage. Audience: UPHSL Junior High School TLE and first-year CIHM culinary students. Unity 6000.6.3f1, uGUI/TextMeshPro; Firebase Unity SDK 13.17.0 Auth and Realtime Database.

## One demonstrable path

MainMenu → RegionalTriviaHub → Batangas / Adobo sa Dilaw → CookingStage → correct cooking → anonymous authenticated UID → atomic RTDB write → matching UID-owned read-back → enabled Continue → heritage response.

Open `SavorPinoyPrototype/` in Unity. Start `Assets/Scenes/MainMenu.unity`; CookingStage requires the resolved trivia handoff. BatangasStage is deprecated. Retained mechanics include practice stopwatch, optional timed challenge, knife strokes, ordered additions, heat retention, vinegar simmer, scoring and trivia rewards. Existing storyboard artwork and kitchen layout remain in use.

Enter a chef nickname; it is only display text. Anonymous Authentication avoids collecting student passwords or school IDs. The UID identifies this installation's account; deleting app data can lose access. Anonymous sign-in is not evidence of a student's real identity.

Finish a round and click **Save attempt**. Loading disables duplicate save clicks. The result shows signing in, saving, acknowledged/read-back, confirmed or retryable error. Continue requires `IsCorrect && SaveSucceeded`; SaveSucceeded also requires the current UID to match the original owner. An incorrect attempt may be saved privately but cannot complete the stage or enter the leaderboard. Practice attempts are private; only correct timed challenges publish leaderboard rows.

**My attempts** queries the current UID's last 20 records. **Top scores** queries the top 10 attempts. Loading disables both query buttons, Empty displays **No chef attempts recorded yet**, and Error explains the failure. Click the same query button to retry. Query text includes stored dish ID, score, attempt key and UTC timestamp. These are explicit refreshes, not live subscriptions.

## Responsibilities

| Script | Responsibility |
|---|---|
| SavorPinoyCookingManager | Scene composition and wiring |
| CookingAttempt | Pure cooking rules and rubric |
| CookingStageController | Session lifecycle and input routing |
| CookingStageView / CookingResultView | UI construction and rendering |
| CookingResultPresenter | Result UI intents and workflow binding |
| AttemptRecord / LeaderboardRecord / AttemptContract | Exact payload fields and validation |
| FirebaseSettings / FirebaseBootstrap | Explicit backend configuration and SDK initialization |
| FirebaseAuthManager | Anonymous identity only |
| AttemptRepository | Atomic write and owned read-back |
| ChefAttemptOutbox | Local pending-record durability only |
| AttemptSaveCoordinator | Save state and completion gate |
| AttemptQueryService / AttemptQueryController | Database queries / query UX states |

## Contract and migration

See [DATA-CONTRACT.md](DATA-CONTRACT.md). `database.rules.json` is the reviewed candidate rules file; a source file is not proof of deployment. The active settings asset contains the exact supplied Singapore URL. `useLocalEmulators` defaults to false and is honored only in the Editor. The isolated test fixture uses a separate `demo-` project, Auth port 9099 and SDK RTDB port 80 (portable rules tests use 9000); it never writes to the live project.

Superseded `PrototypeScoreService`, `PlayerScoreRecord` and `PracticeAttemptStore` source is archived under `Docs/LegacyActivity5/`, outside Unity Assets. Existing `/player_scores` data and old local outbox files are preserved. New rules deny legacy access; no old record is automatically assigned to a UID. Export old data/rules before deployment and migrate only when ownership can be independently established. Historical `Docs/Activity5` evidence and `tools/activity5` tests describe the old schema and do not verify this revision.

The new file `Application.persistentDataPath/savorpinoy-owned-attempts-v2.json` binds each pending payload to a UID before transmission. Same-screen retries retain the immutable `attempt_` key. Timeouts remain unconfirmed even if a late server write eventually succeeds; retry performs another acknowledged write and matching read. Unfinished cooking and automatic pending-queue recovery across app restarts are outside this slice. The queue preserves data for recovery but is not a background uploader. A corrupt queue is preserved and raises an error.

## Verification and live acceptance

Run instructions and results are in [TESTING.md](TESTING.md). Workflow fakes, rules emulators and actual Unity SDK emulator testing have distinct scopes. The live Unity SDK suite also passed all 25 checks against the configured Singapore instance, including visible sign-in and entry guards after owner rule publication. See [live verification and presentation checklist](LIVE-ACCEPTANCE.md). Console screenshots and Android device validation remain separate evidence.

Rules enforce UID ownership, immutable attempts, field/type/range checks, bounded queries, and an atomic leaderboard row matching a correct owned attempt. They cannot prove an untrusted client actually cooked the dish. Score and UTC timestamp originate on the client. Android build/device validation, classroom usability and learning outcomes require separate testing.

See [visible chef sign-in](SIGN-IN.md) for the optional display name, authentication gate and cancellation behavior.
