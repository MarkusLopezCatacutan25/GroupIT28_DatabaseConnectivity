# Verification scope

35 Unity checks passed in an isolated copy with the database URL explicitly set to `offline`. Evidence images are actual runtime captures; they prove gameplay/local storage only. The outbox file was reopened by destroying and recreating the storage service, not by claiming an unperformed full cloud restart.

26 Firebase Realtime Database emulator checks passed using synthetic authenticated contexts, including the same persisted record read by a new client. This does not test live Anonymous sign-in or Unity SDK cloud integration.

Test sources are under tools/activity5. To rerun the rules suite, run `npm install --prefix tools/activity5` to install the pinned test-only dependencies and run from the repository root with Java 17+ and Firebase CLI:

```sh
node tools/activity5/node_modules/firebase-tools/lib/bin/firebase.js emulators:exec --project demo-savorpinoy-activity5 --only database "node tools/activity5/rules.test.cjs"
```

The rules test writes Docs/Activity5/firebase-rules-results.json; set ACTIVITY5_TEST_OUTPUT to select a different output file. Do not run the Unity harness in the production checkout. Copy ValidationRunner.cs into an isolated project's Assets and ValidationEntry.cs into Assets/Editor, set CookingStage Database Url to `offline`, update Begin's output directory, then use Unity batch mode with -executeMethod ValidationEntry.Run. Existing gameplay assets and the three enabled scenes are required. Test assets are excluded from the production game.

Live deployment and required three-way record screenshots remain pending.
