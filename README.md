# Simple Gym Diary

A minimal, no-nonsense workout tracker for Android and iOS (.NET MAUI). Replaces the
paper-notebook workflow: log your working weight (or reps) per exercise, the app marks
each exercise up/down/keep against a target rep range and auto-suggests the adjusted
weight next session.

## Core loop

1. Start a session for a split (e.g. **Upper Body**) — exercises are pre-loaded with
   suggested weights from last time.
2. Enter the reps you managed per set. The arrow badge updates live:
   - below the rep range (default 10–15) → **▼ down** — reduce weight next session
   - within range → **▬ keep**
   - above range → **▲ up** — add weight next session (default +2.5 kg)
3. Tap the badge to override the marking manually. Finish the workout — done.

Everything is saved instantly as you type (no Save button; safe against the app being
killed mid-session — sessions are resumable).

## Features

- **Splits & exercises**: seeded Upper/Lower 2-day preset, fully customizable
- **Two tracking types**: weight-based (machines/barbell) and rep-based (bodyweight)
- **Global defaults + per-exercise overrides** for rep range, increments, counting-set rule
- **Stats**: progression chart per exercise (LiveCharts2), 3M / 1Y / All ranges
- **CSV export**: one row per set, opens directly in Excel
- **kg / lbs** display (storage is always kg)
- Local-only (SQLite), no account, no ads, free

## Solution layout

| Project | Purpose |
|---|---|
| `src/SimpleGymDiary.Core` | Entities, progression engine, SQLite data layer, CSV export — no MAUI dependency |
| `src/SimpleGymDiary.App` | .NET MAUI app (Android, iOS, Windows dev-head), MVVM via CommunityToolkit |
| `tests/SimpleGymDiary.Tests` | xUnit tests for the progression engine, exporter, and database |

## Building

Requires .NET 10 SDK with the `android`/`ios` MAUI workloads (installed via Visual Studio).

```powershell
dotnet test tests/SimpleGymDiary.Tests          # unit + integration tests
dotnet build src/SimpleGymDiary.App -f net10.0-android
dotnet build src/SimpleGymDiary.App -f net10.0-windows10.0.19041.0   # desktop dev-head
```

iOS builds require a paired Mac and an Apple developer account.

## Deferred to v2

Google/Apple sign-in + cloud sync, monetization (planned: one-time Pro unlock),
weighted-bodyweight exercises, drag-to-reorder.
