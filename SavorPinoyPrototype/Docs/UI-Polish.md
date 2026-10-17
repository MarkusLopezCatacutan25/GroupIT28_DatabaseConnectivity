# Scene 2 UI cleanup

Built-in imagegen edited three existing artwork assets into transparent variants. Original source PNGs remain untouched. Clean variants are saved under Assets/Sprites/Scene2: PinClean.png, RecipeCardClean.png, and TriviaFrameClean.png. PNG alpha was inspected before import; bilinear filtering and uncompressed transparency are configured.

Removed the pin's solid button rectangle and moved its CALABARZON label below the icon. The trivia panel no longer has a solid rectangular backing behind its rounded frame. The recipe card uses its complete transparent portrait sprite instead of a fixed pixel crop of the old white-background image. Text uses a locally owned material without the inherited heavy outline. Gameplay, rewards, and timers are preserved.

The full Philippines and CALABARZON map images remain the user's original artwork. This pass does not establish Android device performance or pilot-user usability; those require testing on the actual device and with the pilot group.

## Final prompts used

Pin:

> Edit target: the supplied wooden golden Philippine sun map pin. Remove ALL white background, floor shadow, and white fringe. Preserve the pin, sun relief, pearl, color and silhouette. Single upright pin centered, tightly framed with small transparent padding. Actual transparent alpha outside the pin. No text, no rectangle, no additional objects.

Recipe card (final background-removal pass):

> BACKGROUND REMOVAL ONLY. The target is a portrait bamboo Adobo recipe card. Remove the entire brown background OUTSIDE its rounded bamboo frame, including all four corners. Make that exterior truly transparent alpha zero, NOT brown, NOT white, NOT checkerboard painted pixels. Preserve the complete card border, dish and blank parchment interior. No artistic changes inside card. Small transparent padding around cutout.

Trivia frame:

> Edit target: supplied capiz wooden trivia frame. Preserve wooden/golden frame, capiz pearl inlays, Philippine sun crest at top, pearl bottom and blank opaque parchment center. Remove exterior background and white fringe at every rounded corner. Actual transparent alpha outside frame, fully opaque parchment interior. Tightly frame whole asset with small transparent padding. No text or buttons. Preserve landscape frame proportions and original art style.

## Verification

Compile against installed Unity/uGUI/TMP/Firebase assemblies, run the isolated integration flow, inspect actual Unity renders for exterior corner halos and readable placement, then apply with file-hash checks and backups. Image generation can slightly change painted details; the selected variants retain the established bamboo/capiz/food style.
