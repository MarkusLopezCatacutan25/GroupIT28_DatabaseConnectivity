# Storyboard kitchen redesign

The prep screen now follows the storyboard's illustrated Filipino kitchen: pantry ingredients above a wooden cutting board on the left, burner and palayok on the right, timer/pause in the upper interface, and an Adobo recipe goal beside the stove. Existing cooking rules and score persistence remain in use.

## Assets

Assets/Sprites/Kitchen/KitchenSheet.png is an untouched copy of the supplied Filipino Kitchen Cooking Asset Sheet. Its upper-left scene is used as the kitchen backdrop.

Assets/Sprites/Kitchen/IngredientSheet.png is an untouched copy of the supplied Cute Adobo Cooking Asset Sheet. KitchenArt slices its actual transparent sprites into the board, garlic, chicken, soy bottle, pot, burner, finished dish, and recipe goal. Source rectangles are defined in KitchenArt.cs; pixels are not rewritten or subjected to background removal. The other supplied sheets have opaque white/checkerboard backgrounds and are not used as transparent individual sprites.

Assets/Sprites/Kitchen/Turmeric.png is generated using built-in imagegen because the existing recipe requires luyang dilaw, while the sheet supplies onion. The original ingredients and requirements were not changed to match the sheet. Prompt:

> Game inventory sprite: a small cluster of fresh turmeric roots (luyang dilaw), orange-gold inside, brown-orange knobbly skin, one cut root showing vivid orange cross section. Friendly hand-painted 2D Filipino cooking game style, bold clean dark brown outline, softly shaded warm colors, three-quarter top-down perspective. Centered isolated cluster, actual transparent alpha outside, tight framing with small padding. No onion, ginger-yellow interior, text, label, plate, background, shadow outside, checkerboard or extra objects.

The knife illustration is a simple Unity mesh and has a drag handler. Tap Knife or drag it onto the board after gathering all required ingredients. Follow the green line for the existing six pointer-driven preparation strokes.

## Scene and behavior

CookingStage's Main Camera retains SavorPinoyCookingManager and gains KitchenArt with the two supplied atlas textures and turmeric sprite assigned. Existing GUIDs and serialized database URL are preserved. Runtime sprite slices are disposed when the scene is destroyed.

Gather garlic, turmeric, and chicken. Soy remains the recipe's distractor. After selecting the knife, prep accepts three garlic and three turmeric strokes. Cooking addition buttons replace tool controls after prep. Existing ordered addition, searing, delayed heat response, simmer, no-stir penalty, flame-off plating, grading, retry, local score saves, and heritage views are retained.

The displayed preparation duration stays at 60 seconds or 75 after correct trivia; the storyboard's illustrated timer is a visual reference, not a replacement for the PDF's implemented timing contract. The heat value is a game index rather than Celsius. Device/pilot-user testing and real recipe review remain separate verification steps.

## Try it

Reopen MainMenu after Unity imports. Play through recipe selection and trivia into CookingStage; direct entry still redirects through trivia to obtain a fresh attempt. Gather ingredients, use Knife, trace the green guide, then follow the cooking objective. Do not save an old loaded CookingStage over the changed scene file.

## Validation

Final scripts compile successfully. The isolated Unity run passed 28 checks covering scene flow, knife drag acceptance/rejection, preparation strokes, cooking, failure, grading, heritage, and local score persistence. Screenshots were inspected after the final control-spacing adjustment. Android device and pilot-user testing have not been performed.
