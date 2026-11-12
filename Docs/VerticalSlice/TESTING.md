# Test scope and reproduction

Date: 27 October 2026. Live Firebase acceptance is separate from all local results below.

| Check | Scope | Result |
|---|---|---|
| C# compilation | Actual installed Unity/uGUI/TMP/Firebase assemblies | Passed, no C# diagnostics |
| Full Unity project | Three scenes, serialization/build settings and first-import defaults | Passed |
| Tracked repository preflight | Metadata GUIDs, source layout and LFS policy | Passed |
| Workflow tests | Standalone .NET 9, fake identity/repository gateways | 35 passed |
| Rules tests | Actual Firebase Auth/RTDB emulators, simulated authenticated rule contexts | 46 passed |
| Unity SDK play flow | Actual Unity scene, pointer cuts, Firebase anonymous SDK sign-in and RTDB SDK round trip in isolated local emulators | 17 passed; see evidence/unity-results.json |
| Live Singapore project | Deployed rules, actual authenticated save/read and UI screenshots | 17 live Unity SDK checks passed after owner published the new rules; see evidence/live/verification.json. Earlier denial evidence is retained as history. |
| Android build/device | Mobile package and touch interaction | Not run in this revision |

Workflow tests cover the exact payload keys, identity/display separation, correctness gate, pending writes/read-back, duplicate clicks, UID changes, failed auth, denial, mismatch, timeout/late completion, stable retries, disposed scenes, queue corruption preservation, immutable payload snapshots and query Loading/Empty/Error/retry. These use test gateways and are not network proof.

Rules tests cover valid atomic profile/attempt/leaderboard writes, same-ID retries, unauthenticated/foreign UID access, immutable records/deletion denial, missing/extra fields, malformed values, numeric bounds, required booleans, bounded queries, empty data and cross-linked leaderboard constraints. They do not deploy rules to production.

## Run the portable suites

From the repository root, with .NET 9, Node 22, Java 17 and npm available:

```sh
dotnet run --project tools/vertical-slice/WorkflowTests.csproj -- ./test-output/workflow
npm --prefix tools/vertical-slice install --ignore-scripts
npm --prefix tools/vertical-slice run test:rules
```

Generated test output is disposable; do not commit credentials, emulator logs or node_modules. GitHub Actions runs these two portable suites, not the licensed Unity Editor or the live database.

## Actual Unity SDK evidence

`tools/vertical-slice/UnityValidationRunner.cs` is a test harness kept outside Assets. Copy it into `Assets/Editor/VerticalSliceValidationEntry.cs` **only in an isolated project copy**, preserving source Assets/Packages/ProjectSettings and Firebase binaries. Use a unique company/product name to isolate PlayerPrefs and local attempt files. Set that copy's FirebaseSettings.useLocalEmulators=true. Production settings remain false.

Windows Firebase desktop SDK 13.17.0 derives its cache directory from the database hostname; a host containing `:9000` causes a native initialization failure even when persistence is disabled. The SDK test therefore uses local port 80 with `http://127.0.0.1/`. Its C++ URL parser derives namespace `127` and ignores `?ns=`. The test launcher must install the **same candidate rules explicitly in namespace 127 before starting Unity**. Otherwise a newly created emulator namespace has permissive rules and cannot prove denial behavior. The portable rules suite separately uses the standard demo project namespace on port 9000.

Start Auth/Database emulators with `firebase.unity-emulators.json` and project `demo-savorpinoy-vertical-slice`. The test-only rules initialization is a local HTTP PUT of `database.rules.json` to `http://127.0.0.1/.settings/rules.json?ns=127` with emulator admin header `Authorization: Bearer owner`. This header is a local emulator facility, not a production credential; never use that setup for live verification. Do not enable public production rules.

Set environment variable VERTICAL_UNITY_OUTPUT to an absolute evidence directory, then launch Unity with `-batchmode -projectPath <isolated copy> -executeMethod VerticalSliceValidationEntry.Run -logFile <absolute log>`. The runner exits with a result JSON and screenshots. Do not add `-quit` because the asynchronous play-mode test controls exit. Remove the temporary Editor harness afterward. The fixture retains the authored menu/trivia/kitchen assets; simulation time is accelerated through CookingAttempt.Advance to test the rules without waiting through every cooking second. Screenshot elapsed times are test timings, not human usability evidence.

Local test screenshots and JSON must be labeled **LOCAL EMULATOR ONLY** in any presentation. Known fixture warning: Unity SearchDatabase initialization can report an Editor indexing exception; it is not evidence of a gameplay/database failure. The minimal fixture lacks URP camera components, so Editor screenshots do not establish full rendering-pipeline or Android coverage.

## Visible sign-in addition

The updated suite passed **25 checks against live Firebase** after the visible sign-in was added. It covers unauthenticated direct entry to the hub/kitchen, the visible form, duplicate-click protection, Back/cancellation, authenticated recipe access and carrying the display name forward without re-entry, then the existing cooking/save/query/Continue flow. See [redacted live result](evidence/sign-in/live-verification.json).

The portable suite now also runs **14 sign-in workflow checks**, in addition to the 35 existing save/query checks. It covers error/retry, timeout/late results, cancellation, disposal, invalid names, optional default name and UID mismatches. Those use test identity gateways and do not claim network proof.

The isolated local-emulator attempt passed the sign-in/entry/cooking checks but timed out on the database save. It is not reported as a passing full emulator run for this addition. The subsequent full 25-check live SDK run passed. Prior 46-rule emulator evidence remains valid for the unchanged rules.

For concurrent Editor tests, give the isolated copy its own productGUID as well as company/product name; the original copied identifier caused a Unity Data Store startup conflict while the user's Editor was open. Never change the active project's GUID for this workaround. Existing historical proof reports retain their original scope and dates.
