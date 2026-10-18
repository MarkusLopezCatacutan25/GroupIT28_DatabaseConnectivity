# Activity 5 professor-feedback response

Revision date: 18 October 2026 (Asia/Singapore).

| Professor comment | Change made | Status / evidence |
|---|---|---|
| Actual Unity source unavailable | Current project includes Assets, Packages, ProjectSettings, scenes and scripts. Repository was already at five real commits at review. | Source verified; implementation commit 4574aa9. |
| Complete nickname -> cooking -> save -> retrieve flow | Nickname is entered before cooking; timed challenge retains the result ID; service saves and queries top 10. | 35 Unity checks passed locally. Live Firebase sequence pending. |
| Matching result/Firebase/reopened leaderboard screenshots | Added real local gameplay screenshots and ID display, plus exact capture instructions. | Partially complete. No cloud success screenshot is fabricated. |
| GroupXX, repository URL and visibility | Group 28, exact public URL and complete member names used. | Done; public visibility verified through GitHub API. |
| Unique score_id and created_at type | Node key documented as slice_ plus run GUID; timestamp is an ISO 8601 UTC string. | Done. |
| Required group reflection | Added more than 150 words about attempt, feedback, checks, local results and remaining work. | Draft written; members must review and endorse it. |
| Ownership and field validation | Added Anonymous Authentication, owner_uid, immutable creates, strict fields, bounded queries and score range 0-9375. | 26 emulator checks passed. Live deployment pending. |
| Saving/success/failure and retry | Explicit status messages, durable outbox, same-ID retries, denied-result preservation and recovery. | Local queue/recovery verified. Live failure/retry acknowledgement pending. |
| Expanded README | Exact Unity and Firebase SDK versions, existing project, auth, database URL/rules and MainMenu scene instructions. | Done. |

## Live acceptance sequence

1. Use existing Firebase project savorpinoy-database-daf6f. Supply its exact Realtime Database URL; do not create a new database.
2. Confirm Anonymous Authentication is enabled. Review and publish database.rules.json, preserving unrelated paths.
3. Run MainMenu -> RegionalTriviaHub -> CookingStage; enter a distinctive nickname, choose Timed challenge and complete cooking.
4. Save one record and capture the acknowledged Firebase success message plus record ID.
5. Capture the same node in Firebase Console, including owner_uid and created_at.
6. Stop and reopen the game, reach the result screen, Refresh scores and capture the same record ID. Cached text alone is not retrieval proof.
7. Test a failed request and retry; verify one node exists for that record ID.
8. Add real repository/commit/README, code and deployed-rule screenshots to the report. All group members review the reflection before submitting the same PDF individually.

## Systems thinking (group-review draft)

SavorPinoy demonstrates a system because a visible result depends on several connected components, each with a defined responsibility. The player supplies a nickname and cooking actions through Unity controls. The cooking state machine checks the order of preparation and cooking, updates timing and heat metrics, and produces a score. The result interface presents the outcome and lets the player request a save. Before using the network, the score service writes a pending record to local storage. Firebase Authentication supplies a pseudonymous owner identity. The database rules then check ownership, required fields, allowed values, and score limits before accepting a new record. A separate query retrieves the highest scoring attempts, which the interface sorts and displays. Git stores the actual scripts, scenes and setup information so another person can inspect the connection. A failure in authentication, configuration, permissions or networking can interrupt the cloud flow even when gameplay works. Testing therefore checks component boundaries as well as the cooking interaction. Local tests support implementation progress; the live save-and-reopen demonstration is still required to validate the whole deployed system.

## Group reflection (group-review draft)

Our group attempted to connect a small Adobo sa Dilaw cooking prototype to Firebase so that a player could generate a result, save it, and retrieve it in the game. The professor's feedback showed that our original submission described the intended workflow more clearly than it demonstrated the implementation. At grading, the accessible repository contained only a README and an ignore file, while the report listed scripts and successful operations that the instructor could not inspect. This taught us that a design description, sample records and checked boxes are not substitutes for runnable source code and observed output.

In this revision, we checked the available Unity scenes and scripts, the repository's actual visibility and commit history, the saved database configuration, and the relationship between result data and the leaderboard query. The local Unity validation completed 35 checks, including cooking, scoring, duplicate-save prevention and pending-record recovery. The local Firebase emulator completed 26 checks covering permitted and rejected writes and retrieval from another client context. These checks worked locally; they do not yet prove successful operation against our existing Firebase database.

The remaining difficulty is completing the deployment evidence: supplying the database URL, confirming Anonymous Authentication, publishing the reviewed rules, and recording one matching result in Unity, Firebase and the reopened leaderboard. With more time, we would improve deployment testing, identity recovery and score verification. The repository helps by preserving the implementation and reproducible setup. For our capstone, we will treat evidence, error handling and accurate progress reporting as part of development rather than as tasks to add at submission time.
