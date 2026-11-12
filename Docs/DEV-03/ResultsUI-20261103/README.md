# Trivia and result-screen UX — 3 November 2026

The authored trivia frame and kitchen art remain in use. Trivia now has separate question and answer-review states. After answering, the choices make room for a readable heading, correct answer, explanation, reward and one clear action. Retry restores the question. Correct answers retain the province-specific unlock, preparation bonus and score multiplier.

Results use a large score, three visible rating stars, a dish illustration, three skill bars, elapsed times and an explicit cook → save → next-province checklist. A failed practice attempt clearly explains what to retry, including the screenshot case of 7,300 points with four mistakes and zero stars. Saving a practice record never claims stage completion. The grading rubric has not been relaxed.

Save states are visible: not saved, saving, checking read-back, verified, error and changed profile. A permission failure asks the learner to contact the teacher/project facilitator and offers Retry save. Technical UID and detailed diagnostics remain available in the attempts/history overlay instead of occupying the main gameplay result. Loading disables duplicate outer and inline reload buttons; Empty and Error have their own readable states and an accessible inline retry action.

Continue still requires a correct cooking result and a successful same-UID database write/read-back. The heritage screen has a large dish illustration and an Explore Batangas/Quezon action routed through the presenter and checked again against the verified-save gate. It can reopen after Back to result. Try recipe again preserves the current province instead of randomly switching to another dish.

## Why Pancit could not proceed

The supplied success screenshot had three stars but a failed Firebase save. The owner confirmed that the revised two-dish rules had not been published. The local rules already allow both `Batangas_AdoboSaDilaw` and `Quezon_PancitHabhab`; the Console instance must receive that revision. No rule changes or security bypass were added in this UI task.

1. Open repository-root `database.rules.json`, select all and copy.
2. Open [the exact instance's Rules editor](https://console.firebase.google.com/project/savorpinoy-database-daf6f/database/savorpinoy-database-daf6f-default-rtdb/rules).
3. Replace the rules editor contents and click Publish.
4. Return to a correct Pancit result and click Retry save. Wait for verification, then Continue / next province → Explore Batangas.

If restarting Play after the UI recompiles, start from MainMenu and complete a fresh guided-practice round. Existing pending attempt files are preserved, but a closed result screen is not automatically reconstructed from them. Do not report a pending/local record as a confirmed cloud save.

## Verification scope

The evidence directory contains isolated Unity UI tests using explicit fake gateway identities and responses; its success/error screenshots are presentation checks, not live backend evidence. The test reproduces permission failure, duplicate clicks, write/read-back gating, stable-ID retry, heritage reopening, UID invalidation, query states and the 7,300-point failed-practice case. The production script compile, tracked metadata check and Git LFS check are separate checks. The historical live Firebase reports describe their original release; this task must not claim a new live save until the owner publishes and the live verification is run.

No new AI-generated image was required. The larger Pancit display uses a native Unity sprite rectangle that excludes transparent atlas gutters; the original PNG is unchanged. Android device usability and classroom evaluation are still pending.
