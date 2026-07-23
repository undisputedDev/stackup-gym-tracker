# Simple Gym Diary — project notes for Claude

Workout tracker (.NET MAUI, net10.0) whose key feature is auto-progression: reps vs. a
target range produce an up/down/keep marking per exercise; the next session auto-suggests
the adjusted weight. See [README.md](README.md) for the product overview and
[docs/v1.1-plan.md](docs/v1.1-plan.md) for the agreed next work (5 phases, in order).

## Solution layout

- `src/SimpleGymDiary.Core` — entities, `Progression/ProgressionEngine` (the core logic),
  `Data/AppDatabase` (sqlite-net, migrations), `Data/SeedData` (preset library),
  `Review/ReviewMilestone`. No MAUI references; everything here is unit-tested.
- `src/SimpleGymDiary.App` — MAUI app (MVVM, CommunityToolkit). Pages in `Views/`,
  VMs in `ViewModels/`, `Platforms/Android/DragLift.cs` (native reorder feedback).
- `tests/SimpleGymDiary.Tests` — xUnit; DB tests run against temp-file SQLite.
- `tools/SimpleGymDiary.DemoSeeder` — recreates the Windows-head DB with 3 months of
  demo history (deterministic, deletes the DB first).
- `tools/uia/` — PowerShell UIA drivers for the Windows head (see Verification).

## Commands

```powershell
dotnet test tests/SimpleGymDiary.Tests                                  # 51+ tests
dotnet build src/SimpleGymDiary.App -f net10.0-windows10.0.19041.0     # Windows head
dotnet build src/SimpleGymDiary.App -f net10.0-android -t:Run          # deploy to emulator
& "$env:LOCALAPPDATA\Android\Sdk\emulator\emulator.exe" -avd fmc_pixel # boot emulator first!
dotnet run --project tools/SimpleGymDiary.DemoSeeder                   # reseed demo data
```

- `-t:Run` for Windows breaks (MSB3073/9009: unquoted path with spaces) — build, then
  launch `...\bin\Debug\net10.0-windows10.0.19041.0\win-x64\SimpleGymDiary.App.exe`.
- Full-solution warnings only appear on `--no-incremental` rebuilds.
- iOS never built here (needs a Mac); Android-first.

## Verification workflow (no simulator watching needed)

Windows head: launch exe, then drive it without stealing focus using `tools/uia/`:
`shot.ps1` (PrintWindow screenshot, ×1.25 DPI scale factor), `invoke.ps1 -Name "Start"`
(buttons/tabs), `setvalue.ps1 -EditIndex N -Value X` (entries by index),
`pick.ps1 -ItemName "..."` (combo boxes). Crash diagnosis: DEBUG handler in
`Platforms/Windows/App.xaml.cs` writes `crash.log` to the app data dir
(`%LOCALAPPDATA%\User Name\com.simplegymdiary.app\Data\`).

Android emulator (`fmc_pixel`): screenshot via `adb shell screencap -p /sdcard/shot.png`
then `adb pull` (PowerShell `>` redirect corrupts binary). Taps: `adb shell input tap`.
Drag-reorder: `input draganddrop x1 y1 x2 y2 ms` — NOT `input swipe` with zero distance
(that's dispatched as a tap, not a hold). Transient UI can't be caught by screenshots;
verify with a temporary persistent marker, then revert. Console.WriteLine does not
reach logcat here.

## Hard-won pitfalls (cost real debugging time — don't rediscover)

1. Nested FlexLayouts crash WinUI (stowed exception 0xc000027b, E_FAIL in Measure),
   sometimes nondeterministically. Use Grid + HorizontalStackLayout + horizontal
   ScrollView (see the rep-chip row in SessionPage).
2. LiveCharts2 needs `.UseLiveCharts()` chained in MauiProgram; without it the Stats
   page silently fails to navigate (type initializer throws, Shell swallows it).
3. CommunityToolkit `TouchBehavior` receives NO touch events inside a reorderable
   CollectionView on Android — hook a non-intercepting `RecyclerView.OnItemTouchListener`
   instead (`Platforms/Android/DragLift.cs`).
4. sqlite-net-pcl 1.11.x pairs with `SQLitePCLRaw.bundle_e_sqlite3` 3.x (bundle_green
   is 2.x-only and has a known CVE).
5. `[ObservableProperty]` must use partial properties, not fields (MVVMTK0045 on the
   WinRT target); initializers go in constructors.
6. MAUI 10: use `DisplayAlertAsync`/`DisplayActionSheetAsync`/`ScaleToAsync` (the
   non-Async names are obsolete).
7. Don't batch-edit source files with PowerShell `Get-Content`/`Set-Content` (PS 5.1
   mojibakes UTF-8 without BOM). Use proper edit tooling.
8. LiveCharts pinned to `2.1.0-dev-798` — the only line supporting .NET 10 MAUI;
   don't auto-update.

## Conventions & architecture rules

- **Migrations**: `PRAGMA user_version` + ordered lambdas in `AppDatabase.Migrations`
  (currently v4). New columns always via `AddColumnIfMissingAsync` (fresh installs
  create the full schema in v1, so later migrations must tolerate existing columns).
  Preset seeding (`SeedData.EnsurePresetsAsync`) is idempotent by name and must never
  resurrect user-deleted (archived) rows.
- **History integrity**: `SessionEntry` snapshots exercise name + effective rep range
  at session start; renames/settings changes must never rewrite history.
- **Storage is always kg**; lbs is display-layer only (`UnitConverter`). Parse user
  decimals with `TryParseFlexible` (decimal-comma locales!). CSV/exports would be
  InvariantCulture.
- **Write-through persistence**: session state lives in the DB, never only in VMs;
  every tap persists; process death mid-workout must lose nothing.
- **Settings pattern**: global defaults on the singleton `AppSettings` row + nullable
  override columns on `Exercise`; `EffectiveExerciseSettings.Resolve` is the only
  resolution point.
- Design language: muted teal (#2E6E62), card-based, the arrow marks (▲▬▼ with
  MarkUp/MarkKeep/MarkDown colors) are the visual identity. Icons are hand-drawn
  geometric SVGs (`Resources/Images/icon_*.svg`, white, runtime-tinted). No emojis.
- Commit at feature granularity with detailed messages; PowerShell here-string commit
  messages must not contain double quotes (arg mangling).

## Product decisions (agreed with the owner)

- v1 is local-only (no accounts/sync — Apple sign-in requirement deferred with it);
  everything free, no ads; monetization idea for later: one-time Pro unlock.
- No rest timer, no social, no gamification, no programming features — "simple, mature"
  is the positioning.
- CSV export was built and deliberately removed (git history has it).
- Review prompt: official In-App Review API only, triggered after the finish summary
  at ≥6 completed sessions + ≥1 ▲, max 3 lifetime requests ≥14 days apart
  (`ReviewMilestone`); no custom ask-me-later UI (store policy).
- Feedback address in `SettingsViewModel.FeedbackAddress` is a placeholder — owner
  will create a dedicated mailbox before release (TODO comment marks the spot).
- Next work: [docs/v1.1-plan.md](docs/v1.1-plan.md) — Phase 1 (session-screen clarity:
  rep-chip label, live consequence text, one-time explainer) is first.
