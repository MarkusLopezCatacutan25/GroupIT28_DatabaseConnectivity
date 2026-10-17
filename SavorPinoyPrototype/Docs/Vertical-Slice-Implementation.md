# SavorPinoy Batangas vertical slice

The user's supplied storyboard is the panel baseline. Existing Main Menu and Trivia Hub sprites are retained. Runtime spacing and safe-area bounds keep the authored Trivia Hub controls readable; the artwork is not replaced. Cooking artwork is temporary, drawn using Unity UI shapes pending the team's kitchen sprites.

## Implemented flow

MainMenu -> RegionalTriviaHub (recipe selection, then trivia) -> CookingStage (preparation, sequence, simmer, result) -> retry through trivia or return to menu.

| Basis panel | Implementation in this increment | Requirements |
|---|---|---|
| P1 | Existing Main Menu assets, Play, volume options, Exit, safe-area content | REQ-02/03/04, DES-01/03/04 |
| P2 | Existing map asset with the one selectable Batangas recipe | REQ-01/02/04, DES-01/03/04 |
| P3 | Three dish-related question variants; 15-second countdown; correct/wrong/timeout feedback; one-shot handoff | REQ-01/02/04, DES-01/03/04 |
| P4 | Garlic/turmeric/chicken selection, soy sauce distractor, knife/spoon matching | REQ-01/02/03, DES-01/04 |
| P5 | Six guided pointer strokes; local normalized alignment and stability evaluation | REQ-02/03, DES-01/03/04 |
| P6 | Delayed palayok heat, garlic -> turmeric -> chicken -> timed sear -> vinegar -> unstirred simmer -> flame off -> plate | REQ-01/02/03, DES-01/04 |
| P7 | Timer/scorch/low-performance failure, cause-specific feedback, clean retry | REQ-02/04, DES-01/03 |
| P8 | Successful plating presentation and stars; terminal result is immutable | REQ-02/04, DES-01/03 |
| P9 | Component score breakdown, explicit trivia multiplier, durable local record, optional cloud submission/retrieval | REQ-02/03, DES-01/02/03/04 |
| P10 | Daily meal tracker remains outside this 10% increment | REQ-01/02, DES-02/03 |
| P11 | Batangas heritage card opens from results; no unsupported historical/nutrition claims added | REQ-01/02/04, DES-03 |
| P12 | Persistent tool upgrades remain outside this 10% increment | REQ-02, DES-01/02/03 |

The storyboard's captions contain generic Chicken Adobo/soy-sauce examples; this selected Adobo sa Dilaw prototype uses turmeric and vinegar instead. Its recipe is a simplified training procedure requiring culinary-adviser validation, not real-world cooking/safety instructions. Heat units are an abstract index, not degrees Celsius. Four-second searing and twelve-second simmering are simulated durations.

## Scoring and timer contract

- Prep: 60 seconds; correct trivia grants 15 more seconds. Prep expiry fails. The separate cooking limit is 75 active-play seconds.
- Correct trivia grants a 1.25 multiplier; wrong and timeout both resolve successfully with no reward. Scene entry consumes the handoff once. Retry returns through a new trivia attempt.
- Cuts: three garlic cuts followed by three turmeric preparation strokes. Each requires a top-to-bottom stroke with valid start/end positions and >=75% alignment/stability. Taps, reverse strokes, out-of-board strokes, duplicate/post-terminal actions, and nonfinite coordinates cannot award cuts.
- Cutting score: mean accepted accuracy x 2,500. Sequence score: max(0, 2,500 - 250 x mistakes). Thermal score: max(0, 2,500 - floor(150 x cumulative scorching seconds)). Raw score: max(0, sum of components - action penalties).
- Wrong ingredient/tool: -100 points and -5 prep seconds; wrong sequence/missed cut: -100 points; premature stirring: -500 points. Mistakes also affect the sequence component and star gate. Already-selected ingredients do not score again.
- Stars are awarded on raw cooking performance: 3 stars requires >=7,000, zero mistakes, >=85% accuracy, and zero scorch warnings; 2 stars requires >=5,500, <=1 mistake, and >=70% accuracy; 1 star requires >=4,000 and <=2 mistakes. Otherwise the attempt fails the rubric.
- Final successful score rounds raw score x trivia multiplier once, away from zero. Maximum is 9,375 (7,500 x 1.25). No +500 additive trivia reward. This deliberately follows the user's prototype reward and differs from the expanded GDD's 8,000/+500 formula. Failed attempts are not uploaded or ranked.
- Pausing/backgrounding freezes active gameplay; the player resumes explicitly. Process restart abandons unfinished attempts but preserves submitted local records. Competitive timing/anti-cheat is not claimed.

## Local scores and Firebase

Scores are queued only after the player enters an alias and clicks Save. The durable envelope includes component evidence, rubric, immutable payload, and stable record ID. Writes use a temporary file with backup recovery. Repeated Save for the same run cannot generate another row. The persistent service survives scene exits and reconciles existing same-ID cloud records before attempting another write.

Cloud compatibility retains `/player_scores/{slice_runId}` and the existing six-field PlayerScoreRecord: player_name, score, dish_name, level, remarks, created_at. created_at is a UTC ISO string here to avoid an unapproved schema migration; component metrics remain local. remarks identifies `batangas_slice_v1`. Existing deployed rules and SDK/auth configuration were not changed. Cloud scores are client-reported and cannot be treated as trusted assessments.

The saved project lacks an exact database URL. CookingManager's databaseUrl stays blank unless supplied. Offline play, feedback, local records, and heritage cards work without it. No remote test records are sent by the validation fixture. After the real URL is supplied, verify live connection and the actual deployed read/write/type/range rules separately.

Leaderboard retrieval orders numeric score and requests the last ten records, displayed descending with a deterministic key tie-break. It is a one-shot refresh, not a realtime subscription. The shared legacy collection may contain different scoring scales; this is disclosed in the view. A rubric-separated authenticated board and best-per-player summary need a later schema/rules migration.

Ten-second UI budgets distinguish pending submission or cached/read errors from an empty leaderboard. Transient sends retry with backoff and stable identity; rejected records remain local. Callback UI is subscribed only while the scene exists. The direct-client legacy service is not an authorization or anti-spam solution.

## Compilation repairs and preservation

PlayerScoreRecord1 and the two prev_backup class files duplicated active class names inside Assets. Their original contents are preserved behind `#if false` rather than deleted. Existing script/scene/sprite GUIDs are retained. All applied files are backed up outside the active project's Assets folder, so backups cannot introduce duplicate compilation again.

## Verification

- Behavioral tests cover trivia outcomes/late input, exactly-once handoff, fresh retries, prep expiry, duplicate gathering, invalid sequence, timed searing, simmer heat, stirring penalty, thermal inertia/scorch failure, immutable results, and pointer geometry.
- Script compilation is checked against the installed Unity 6000.6.3f1, uGUI/TMP, and Firebase assemblies.
- An isolated Unity project tests real scene transitions, custom option bindings, rewarded timer, six pointer-driven cuts, searing/simmer/plating controls, result/heritage views, and offline record deduplication. Actual Unity Canvas renders are inspected.
- Physical Android touch/notch behavior, IL2CPP APK performance, deployed Firebase rules, network acknowledgement/reconnection, and online ranking are still device/service verification tasks. Temporary kitchen graphics are not final production art.

## Try the slice

Reopen MainMenu from Assets/Scenes after Unity imports the changes; avoid saving an old loaded scene over the updated file. Play -> Select Recipe -> Answer -> Proceed. Gather garlic, luyang dilaw, chicken and choose Knife. Trace six marked strokes from top to bottom. Move the flame slider to about 60%; wait for pot readiness. Add garlic, turmeric, chicken; wait for searing; add vinegar; keep the heat in the displayed band without stirring. After simmering, turn the flame off and plate. Enter an alias and save; retry always returns through trivia.

Confirm the active Android Build Profile uses MainMenu, RegionalTriviaHub, CookingStage in that order; a profile-specific override may supersede the global scene list.
