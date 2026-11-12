> 8 November completion update: see [prototype art and level presentation completion](Completion-20261108/README.md). This supersedes the earlier pending vessel states, steam loop, and recipe-card artwork items below for the existing two-province prototype. The existing kitchen is retained as the final prototype environment. Android device and student/instructor acceptance remain separate.

# DEV-03 — first production sprite batch (31 October 2026)

> 3 November update: two-province trivia and preparation changes are described in [the November release notes](November03/README.md). Quezon live saves require publication of the revised rules; October live evidence is historical.


## Implemented

Three user-supplied Gemini images were derived into transparent production sprites using the built-in imagegen background-extraction workflow. The original RGB/magenta/checkerboard source files remain unchanged in the Desktop CAPSTONE art folder. The game consumes versioned sibling PNGs under `SavorPinoyPrototype/Assets/Sprites/Kitchen/Dev03/`.

| Sprite | In-game use |
|---|---|
| GarlicPrepared_v02.png | Appears after accepted garlic cuts and remains on the board during turmeric preparation; displayed on the prepared tray. |
| TurmericPrepared_v02.png | Appears after accepted turmeric preparation cuts; displayed on the prepared tray. |
| AdoboDilawPlated_v02.png | Golden dish on a successful result; hidden for failed attempts. |

KitchenArt owns the three serialized sprite references and keeps older art fallbacks. CookingStageView presents the prepared piles with a short scale pulse following accepted cuts. CookingResultView presents the completed dish only for success. The current environment, chopping-board angle, knife, raw pantry sprites, palayok and existing procedural steam remain in use. No recipe, scoring, timer, authentication, database or completion-gate rules were changed.

## Texture preparation

The received copies are RGB and have no real transparency. The derived production files are RGBA with both fully transparent and opaque pixels; the asset metrics report records the alpha check. No visible magenta pixels were detected by the stated color threshold. These are generative cleanup outputs, not a claim of pixel-identical extraction. Original-resolution files are preserved; Unity import is capped at 1024 pixels on the longest edge for this UI usage, with Single Sprite, alpha transparency, bilinear filtering, clamp wrapping and no mipmaps. Sprite proportions remain preserved.

## Verification

The actual Unity art/presentation preview passed **17 checks**: import/reference resolution, transparency import settings, no prepared artwork before cutting, rejected-stroke handling, accepted-cut sprite transitions, completed preparation tray, unchanged cooking success path, successful dish display, failed-dish hiding, and Continue remaining locked without save confirmation. Screenshots and result JSON are in `evidence/`.

This preview disables network composition and uses an explicit editor-only mock identity named `editor-art-preview`. It does not sign in, write records or query Firebase and is not cloud verification. Prior live integration proof remains separate in Docs/VerticalSlice. Preview cooking time is accelerated; the screenshots are not classroom usability evidence.

To reproduce, use an isolated project copy with its own product GUID. Copy `tools/vertical-slice/Dev03ArtValidationRunner.cs` into the root Assets folder of that copy (outside Editor, never production). Set DEV03_ART_OUTPUT to an absolute output directory. Launch Unity in batch mode with executeMethod Dev03ArtValidationEntry.Run and no `-quit`; the runner exits itself. Its in-memory scene edit disables the network composition in the fixture only and is not saved to the source scene.

## Still pending for DEV-03

- Consistent separate palayok empty/simmering/burnt states and lid layers.
- Clean, aligned steam animation frames and a verified loop.
- Any replacement environment layers from the new kitchen concept.
- Updating the older baked recipe-card illustration to match the new plated dish.
- Android device art/performance and target-student usability review.

This completes the first sprite-integration batch, not the full DEV-03 phase. See image-cleanup-prompts.json for the exact cleanup prompts and asset-manifest.json for provenance.
