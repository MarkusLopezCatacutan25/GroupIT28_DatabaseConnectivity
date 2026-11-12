# Live vertical-slice verification � passed 27 October 2026

The owner enabled Anonymous Authentication and published the candidate rules in the exact Singapore Realtime Database instance. The actual Unity SDK test then passed all 17 integration checks against the live backend, with emulators disabled. Earlier denied-save evidence is retained to show that Continue stayed locked before effective rule deployment.

- Live instance: `savorpinoy-database-daf6f-default-rtdb.asia-southeast1.firebasedatabase.app`.
- Test nickname: **VerticalSliceChef**.
- Authenticated owner UID: verified; omitted from repository evidence.
- Attempt key: verified; omitted from repository evidence.
- Dish: `Batangas_AdoboSaDilaw`; score: **9375**; is_correct: **true**.
- Evidence: [redacted live verification](evidence/live/verification.json). Original screenshots and identifiers remain in local task output, outside the repository.

The test demonstrated actual pointer chopping and cooking rules, anonymous UID distinct from nickname, acknowledged atomic write, matching read-back, query Loading/Success/Empty/Error, cross-UID denial, retry recovery and gated Continue. A new repository object retrieved the same record within the test process. The harness accelerated cooking time for technical verification; this is not student usability evidence. The high-scoring test entry remains identifiable on the live leaderboard. The harness signed out and created a second anonymous test UID to verify isolation and empty history.

Firebase CLI remains unsigned-in; rule publication was performed by the owner through Console. Full Android build/device validation and a separate fresh-process retrieval demonstration were not performed. Capture an instructor-facing Console screenshot of the same owned attempt and leaderboard key; automated UI/SDK evidence does not itself show the Console page.

## Manual reproduction / presentation checklist

1. Firebase Console → Authentication → Sign-in method → enable **Anonymous**. Do not use a nickname as authentication or create student passwords for this slice.
2. Realtime Database → verify the instance URL matches the Singapore URL in DATA-CONTRACT.md. Export existing data and copy existing rules as a backup. Review root `database.rules.json`; it locks legacy `/player_scores`. Preserve unrelated application paths if this instance is shared. Publish the reviewed rules to this exact instance.
3. Unity → select `Assets/Settings/FirebaseSettings.asset`: exact Singapore URL, **Use Local Emulators unchecked**. Keep existing Firebase application configuration. Open MainMenu, Play, choose Batangas, answer trivia and enter a distinctive test nickname.
4. Complete the dish. Capture a correct result with Continue disabled. Click Save attempt. Capture the actual **Saved to Firebase and read back successfully** response, authenticated UID and enabled Continue. A pending local file or emulator result does not qualify.
5. Click My attempts. Capture the stored attempt key, dish ID, score and timestamp. In Console Data, expand `users/<same uid>/attempts/<same attemptId>` and match all five fields plus profile display name. For a correct timed challenge, match all five fields under `leaderboard/<same attemptId>` too.
6. Stop/restart the game without clearing app data, complete another round and load My attempts. Verify the original key is retrieved under the same UID. Capture the UI, Console data, active rules and code/commit reference together.
7. Exercise a denied request or unavailable network. Confirm Continue stays locked, no false Saved message appears, and retry after recovery retains the same attempt key. Distinguish deliberate test failures from real student records. Do not weaken production rules to make a demo pass.
8. Verify Empty and Error/retry query states, then click Continue for the confirmed correct attempt. Record PC Editor scope separately from Android device/build scope.

Optional CLI alternative after the owner signs in interactively: from the repository root, `firebase deploy --only database --project savorpinoy-database-daf6f`. Inspect `firebase.json` and instance targeting first. Never commit login tokens or service-account files. The Console steps above require no CLI account and are sufficient for this prototype.
