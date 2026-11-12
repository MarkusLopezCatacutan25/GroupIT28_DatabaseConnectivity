# Exact Realtime Database contract

> 3 November update: two-province trivia and preparation changes are described in [the November release notes](../DEV-03/November03/README.md). Quezon live saves require publication of the revised rules; October live evidence is historical.


The backend is Realtime Database, not Firestore. Database URL:
`https://savorpinoy-database-daf6f-default-rtdb.asia-southeast1.firebasedatabase.app/`

```json
{
  "users": {
    "<authenticated uid>": {
      "display_name": "ChefMaria",
      "updated_at": "2026-10-27T10:00:00Z",
      "attempts": {
        "attempt_0123456789abcdef0123456789abcdef": {
          "dish_id": "Batangas_AdoboSaDilaw",
          "score": 950,
          "is_correct": true,
          "remarks": "batangas_slice_v1 | 1 stars",
          "created_at": "2026-10-27T10:00:00Z"
        }
      }
    }
  },
  "leaderboard": {
    "attempt_0123456789abcdef0123456789abcdef": {
      "uid": "<authenticated uid>",
      "player_name": "ChefMaria",
      "dish_id": "Batangas_AdoboSaDilaw",
      "score": 950,
      "created_at": "2026-10-27T10:00:00Z"
    }
  }
}
```

This is illustrative schema data, not a real saved record. `attemptId` is a node key, never an extra payload field. It is `attempt_` plus 32 lowercase hexadecimal characters. AttemptRecord has exactly five fields; LeaderboardRecord has exactly five fields. No `owner_uid`, `dish_name` or `level` is sent in the new payload.

| Field | Type / rule | UI or ownership mapping |
|---|---|---|
| users key / leaderboard.uid | Firebase Auth UID | Displayed separately from nickname; only that UID writes |
| display_name / player_name | 1–24 allowed nickname characters, at least one letter/number | Chef nickname / leaderboard label |
| dish_id | String, exactly Batangas_AdoboSaDilaw | Stored recipe ID shown in query rows |
| score | Integer 0–9375 | FinalScore; shown in results and queries |
| is_correct | Boolean | Success phase; required with confirmed save to continue |
| remarks | String matching current rubric and 0–3 stars | Skill evaluation metadata |
| created_at / updated_at | UTC string yyyy-MM-ddTHH:mm:ssZ | Client clock, format checked |

Raw score maximum is 7500 (cutting 2500 + sequence 2500 + thermal 2500); correct trivia applies 1.25×, yielding 9375. Penalties reduce score. A sample score of 950 does not change that rubric.

AttemptRepository uses one root `UpdateChildrenAsync` for profile, owned attempt and eligible leaderboard row. Rules compare the leaderboard against the post-write owned record and display name. Attempts and board rows are immutable except identical idempotent retries; deletion, extra fields, foreign UIDs and unauthenticated requests are denied. Profile fields can update only under the caller's UID. History requires ordered `created_at` with LimitToLast ≤20; shared scores require ordered `score` with LimitToLast ≤10. Parent private-user reads are denied.
