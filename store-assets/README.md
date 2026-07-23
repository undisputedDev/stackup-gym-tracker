# Store assets — StackUp

Marketing/listing assets for the Google Play Store. See
[docs/play-store-release.md](../docs/play-store-release.md) for how these are used.

## Screenshots

Captured from the Android emulator (`fmc_pixel`, 1080×2400 phone) running a demo
database seeded with ~3 months of history (`tools/StackUp.DemoSeeder`). Two languages,
because the listing is bilingual (English default + German).

```
screenshots/
  en/
    raw/         1..5 = the 5 store screens, uncaptioned (+ 6_explainer bonus)
    captioned/   same screens with a teal (#2E6E62) caption band on top
  de/
    raw/         German UI + German exercise/split names
    captioned/   German captions
```

**Which to upload?** The `captioned/` set is recommended for the listing (higher
conversion). The `raw/` set is the fallback if you prefer the clean look. Each image is
1080-px wide and satisfies Play's phone-screenshot rules (16:9 / 9:16, ≥320 px). Upload
2–8 per language.

### Screens and their captions

| # | Screen | EN caption | DE caption |
|---|--------|-----------|-----------|
| 1 | Workout home (splits + up/keep/down summary) | Pick a workout and start | Training wählen und loslegen |
| 2 | Session in progress (▲ up + "next time" hint) | Your next weight, suggested automatically | Dein nächstes Gewicht – automatisch vorgeschlagen |
| 3 | Stats (progression charts) | Watch every lift trend upward | Dein Fortschritt, Übung für Übung |
| 4 | Splits library | Ready-made splits, fully customizable | Fertige Splits, voll anpassbar |
| 5 | Finish summary ("next time" weights) | Finish – next session plan included | Abschließen – Plan fürs nächste Mal |
| 6 | (EN only, bonus) "How progression works" explainer | — | — |

## Still to create (see the release guide, Step 5)

- **App icon** — 512×512 PNG (export from `src/StackUp.App/Resources/AppIcon/appicon.svg`).
- **Feature graphic** — 1024×500 PNG (teal background, "StackUp" + tagline + ▲▬▼ motif).

## Regenerating the screenshots

1. Boot the emulator, deploy the app (`dotnet build src/StackUp.App -f net10.0-android -t:Run`).
2. Seed a demo DB in the desired language and push it into the app's data dir:
   `STACKUP_SEED_CULTURE=en dotnet run --project tools/StackUp.DemoSeeder -- <path>` then
   `adb push` → `adb run-as com.stackupgym.app cp … files/gymdiary.db3`.
   For German: set the emulator locale (`adb root; adb shell setprop persist.sys.locale de-DE; adb reboot`)
   and seed with `STACKUP_SEED_CULTURE=de`.
3. Capture with `adb shell screencap -p /sdcard/s.png` + `adb pull` (binary-safe).
4. Add caption bands with `store-assets/tools/caption.ps1` (GDI+ compositor; teal band +
   white text). Edit the `$base`/`$shots` paths at the top to point at your captured PNGs.
