# Visible chef sign-in

The prototype now makes authentication an explicit step before recipe selection:

**MainMenu → Play → Chef sign-in → Continue as guest → Firebase Auth UID → RegionalTriviaHub → CookingStage → confirmed save/read-back → Continue.**

The existing Anonymous Authentication provider remains in use. The screen does not ask for email, password, school ID or a real name. A chef name is optional display text; blank input becomes **Chef**. It does not create an identity or authenticate a student. The Firebase SDK supplies the actual UID. A guest profile is associated with the local installation and is not a portable student account.

The main-menu background and logo are reused. During sign-in, the submit button and name field are disabled and a loading message is displayed. Failure or timeout keeps the player on the sign-in screen with Retry. Back cancels navigation; a late SDK completion cannot silently send the player into the game. Entering a nickname alone never grants access. Cached SDK guest accounts are reused, but the visible Continue as guest action is still required each time the player enters from MainMenu.

After successful sign-in, the chef name is carried into the cooking mode selector as read-only text. To change the display name, return to MainMenu and open sign-in again. Changing this display name does not switch the authenticated account. PlayerPrefs stores only the display preference, never a UID as authentication proof.

Directly opening RegionalTriviaHub or CookingStage without the in-memory access ticket returns to MainMenu. A ticket is issued after confirmed sign-in, is cleared when entering MainMenu or starting a new play session, and becomes invalid if the SDK's current UID changes. The existing database rules and correct-result/save-success gate remain authoritative for persistence and completion.

Responsibilities are separated: ChefSignInView renders widgets, ChefSignInCoordinator manages loading/error/cancel/confirmation state, ChefSignInController routes input and the scene transition, ChefAccessSession holds the session access ticket, and FirebaseAuthManager owns SDK identity. FirebaseBootstrap shares one dependency check across scene compositions and opens the database lazily after authentication.

This is visible guest sign-in, not email/password registration or verified student identity. It follows the guideline's permitted Anonymous Authentication option. No new Firebase provider or rule deployment is required for this UI addition.

## Verification

Fourteen sign-in workflow checks and the existing 35 workflow checks pass. All 25 checks in the actual live Unity SDK integration suite pass, including both direct-scene guards and the sign-in-to-save flow. The local emulator save timeout is documented separately in TESTING.md. Android device validation is still separate.
