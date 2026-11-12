# Two-province learning flow — 3 November 2026

This update extends the playable prototype with province-specific trivia, ordered preparation gestures, drag-and-drop additions, and focused stove/container views. The existing storyboard kitchen art remains the base.

## Play path

Open `SavorPinoyPrototype/Assets/Scenes/MainMenu.unity` and enter Play. Use visible guest sign-in; the optional chef name is centered and remains display text, separate from Firebase UID. Select Play and tap the CALABARZON pin. A cloud curtain covers the map switch. The first province is randomly selected between Batangas and Quezon. Each province has two questions; one is randomly drawn per attempt. A wrong answer or timeout shows an explanation and retry. A correct answer permits its matching recipe.

Choose Guided practice for an elapsed-time stopwatch without expiry. Timed challenge now allows four minutes of preparation plus the existing trivia bonus; cooking retains its 75-second abstract challenge timer. These are game timers, not recommended kitchen cooking times.

Batangas uses Adobo sa Dilaw. Prepare hands, separate garlic, remove skins, slice garlic, wash/peel/slice turmeric, switch to a separate meat board, portion boneless chicken, and clean after handling raw chicken. There are nine representative accepted cuts plus the non-cut gestures. Quezon uses a clearly labeled simplified **vegetable learning adaptation** of Pancit Habhab: prepare garlic, onion, carrot planks, carrot strips and cabbage. There are fifteen representative accepted cuts. The station's measured oil/seasoning mise en place is assumed in this simplified lesson; it does not teach ingredient quantities or the complete traditional meat recipe.

During cooking, drag ingredient pictures into the highlighted container opening. Click-to-add buttons are removed. A container close-up opens for the drag, the palayok lid lifts, and a valid drop animates inward before the domain receives one addition. An outside drop returns the ingredient without progress. Adjust heat opens a centered stove view with a larger slider. Food sprites, simmer progress and steam provide visual feedback; these are approximate game cues, not a thermometer or physical cooking model.

The two recipes have different addition sequences. Batangas retains garlic → turmeric → boneless chicken → simulated sear → vinegar → simulated initial unstirred simmer. Quezon uses garlic → onion → vegetables → simulated stir-fry → seasoned vegetable broth → miki noodles → simulated noodle cooking. The recipe data, preparation progress, input bindings, visual focus and persistence contract are separate scripts.

After correct cooking, **Save attempt** must still receive a real write acknowledgement and matching UID-owned read-back before Continue opens the heritage response. From there **Next province / trivia** opens the other province. Unsaved or incorrect results cannot use that completion path. Initial random selection means either province can be encountered first; there is no persistent campaign-unlock claim.

## Firebase rule publication is required for Quezon

The local `database.rules.json` now permits exactly `Batangas_AdoboSaDilaw` and `Quezon_PancitHabhab` in both attempt and leaderboard dish fields. Ownership, immutable records, five-field payloads, maximum score 9375, private history and bounded queries remain enforced. No legacy data migration or deletion was performed.

The owner must publish this revised file to the Singapore instance before a live Quezon save can be verified. This task did **not** deploy live rules or rerun the historical live Firebase proof. A permission-denied Quezon save must show retry and keep Continue locked; it must not be presented as saved.

1. Open the repository-root `database.rules.json` and copy its full contents.
2. Open Firebase Console → project `savorpinoy-database-daf6f` → Realtime Database → instance `savorpinoy-database-daf6f-default-rtdb` → Rules.
3. Replace the editor contents with that file and click Publish.
4. Play each province, complete cooking, save, verify its matching `dish_id` under your authenticated UID, reload My attempts, and confirm Continue unlocks only after verification.

## Evidence and limitations

The evidence folder contains isolated Unity interaction screenshots/results, pure domain/fake gateway workflow results and local Firebase rules-emulator results. The UI test identity is deliberately `isolated-ui-test`; no production user data is included. These tests do not prove deployed rules, Android device performance, classroom usability or learning effectiveness. Earlier October reports describe the earlier release; the new interaction runner supersedes their six-cut/button-based presentation harness.

Preparation uses representative gestures and sprite states. It does not yet depict anatomically animated hands, individual skin particles, continuous knife/ingredient deformation, bone cutting, real sanitation or measured ingredient quantities. CIHM instructor review is required before making claims that the lesson teaches a complete real-kitchen procedure. Android/touch device testing and target-student testing remain pending.

## Art

`Assets/Resources/Art/PreparationAtlasV1.png`, `PreparationDetailsV1.png`, and `EmptyPalayokV3.png` are new transparent PNG derivatives generated with the built-in image tool. The exact prompts and source generation filenames are in `image-prompts.json`. Runtime sprite rectangles select equal atlas cells; no old artwork was overwritten. Existing kitchen, close-up board, knife, prepared garlic/turmeric and golden Adobo result sprites remain in use. The lid and travel clouds are native animated UI meshes.

## Content sources

- Existing program: Batangas Barako and turmeric questions, retained for this release.
- [National Museum: traditional food wrappings](https://www.nationalmuseum.gov.ph/2022/04/30/traditional-food-wrappings-in-the-philippines/) supports banana-leaf association with Quezon Pancit Habhab.
- [Philippines tourism brochure hosted by the Philippine Embassy](https://philippineembassy-dc.org/wp-content/uploads/2023/02/e-version-PH-info-brochure-2.pdf) describes Lucban's Pancit Habhab and banana-leaf serving style. The two Quezon questions are new additions, not claimed to have existed in the old program.
- [Ajinomoto Pancit Habhab recipe](https://www.cookmunitybyajinomoto.com/recipes/pancit-habhab/) informed the order of aromatics, vegetables, broth and noodles. This game's vegetable adaptation is explicitly simplified.
- [FoodSafety.gov: four food-safety steps](https://www.foodsafety.gov/keep-food-safe/4-steps-to-food-safety), [USDA: separate cutting boards](https://ask.fsis.usda.gov/article/How-can-I-prevent-cross-contamination-when-using-cutting-boards), and [USDA: do not wash poultry](https://ask.fsis.usda.gov/article/Should-I-wash-chicken-or-other-poultry-before-cooking) informed hand hygiene, separation, cleanup and thermometer guidance. Simulation heat indices and seconds must not replace those real practices.
