# Scene 2: supplied artwork and transitions

Reopen Assets/Scenes/RegionalTriviaHub.unity after Unity imports the changes, or start from MainMenu.

## Artwork now assigned

The three attached original PNGs are imported under Assets/Sprites/Scene2 as ArchipelagoMap, CalabarzonMap, and AdoboRecipeCard. RegionalMapZoomManager on Canvas references these sprites directly. The earlier framed regional map is no longer the overview/focused map.

The recipe PNG includes a white outer margin. Its original pixels are preserved. The controller creates a sprite rectangle (318,18,389,534) from the 1024 x 572 texture to display the portrait card without that margin. Dish illustration occupies the upper half; title, regional context, and Select Recipe are placed in the blank lower half. This crop is specific to the supplied image; revise it when replacing that image.

The pin is positioned at normalized (.443,.612) on the supplied full-map image, visually aligned to southern Luzon. This is a UI location on an illustrated map, not a surveyed geographic coordinate.

## Flow

1. Overview shows the full Philippine map in a masked viewport, with the IV-A pin. Recipe and trivia are hidden and the countdown is stopped.
2. Pin tap animates scale/position toward southern Luzon. At completion, the detailed CALABARZON sprite replaces the overview. The supplied portrait recipe card slides in on the right. Back to Overview cancels/reset safely.
3. Select Recipe retracts the card, transitions into equal map/trivia panels, and starts the fifteen-second trivia timer only after the transition finishes.
4. Correct answers grant fifteen preparation seconds and a 1.25 final-score multiplier. Incorrect/timeout grant neither. Proceed to Kitchen Prep loads the existing CookingStage without clearing the handoff during scene unload.

## Inspector

Canvas has RegionalTriviaManager and RegionalMapZoomManager. Existing references to custom question, option buttons, feedback, timer, and Proceed remain intact. Archipelago Map, Calabarzon Map, Recipe Preview Card, and Region Pin are assigned. Transition Seconds=.65. No duplicate OnClick bindings are needed; listeners are managed by code.

Runtime hierarchy:

```text
Canvas (1920 x 1080 reference, Match .5)
  Background (existing custom frame)
  SafeAreaRoot (Screen.safeArea)
    Scene2Design (1840 x 1000, uniform fit)
      RegionTitleText / TimerBadge / BackToOverview
      SplitContainer (1760 x 880)
        LeftMapPanel (Image, RectMask2D)
          MapContent (Image, Preserve Aspect)
            CalabarzonPin
        RightTriviaPanel (Image, CanvasGroup)
          TriviaModal (existing custom artwork)
            QuestionText / OptionsContainer / FeedbackBanner / ProceedToKitchenBtn
      RecipePreviewCard (supplied portrait artwork, CanvasGroup)
        RecipeTitle / RecipeRegion / ChooseBatangas
```

Centered anchors/pivots own the transition. Final split panels are each 880 x 880 at (-440,0) and (440,0). MapContent shrinks with the left panel, preserving the supplied map aspect ratio; ocean-color backing fills unused space. A HorizontalLayoutGroup is deliberately not used because it would override animated widths/positions. Existing options retain their VerticalLayoutGroup. Safe-area uniform fitting retains the composition on wider landscape screens; physical Android touch/notch testing remains pending.

Question font=32, option font=27, feedback font=24, header font=28. Title and regional context are positioned below the dish on the recipe card. ResetMapZoom cancels the current view/trivia and resets bonuses. LoadTriviaForRegion("CALABARZON") rejects duplicate or unsupported starts.

## Verification

The scripts compile against installed Unity/TMP/uGUI/Firebase assemblies. The isolated Unity integration fixture tests transitions, timer inactivity before trivia, repeated pin taps, zoom cancellation, duplicate start prevention, paused timers, incorrect/timeout/correct outcomes, reset, and the complete kitchen/result/offline-save flow. Actual Canvas renders of overview, recipe preview, trivia, and feedback are visually inspected using the supplied assets. This does not establish physical Android compatibility or deployed Firebase behavior.

Coffee-question content source: Department of Agriculture [Batangas Coffee Day report](https://hvcdp.da.gov.ph/2023/10/27/batangas-coffee-day-2023/). Simulated heat explanations remain identified as game behavior.
