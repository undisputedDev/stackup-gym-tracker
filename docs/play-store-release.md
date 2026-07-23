# Shipping StackUp to the Google Play Store

A first-timer's step-by-step guide to publishing **StackUp – Simple Gym Tracker** on Google
Play. Follow it top to bottom. Boxes marked ☐ are things *you* do in a browser or on your
phone; the build commands are things you run on this PC.

> **Do this first (it has the longest lead time):** Google verifies every new developer's
> identity, and that can take **a few days**. Start **Step 1** now so the wait overlaps with
> everything else.

---

## What's already done in the repo (by the code changes accompanying this guide)

- ✅ Target API level is 36 (the .NET 10 Android workload default) — meets Play's target-API requirement (min accepted is API 35). minSdk stays 21.
- ✅ Release build configured to output an **`.aab`** (Android App Bundle) and to sign with
  your keystore (`StackUp.App.csproj`).
- ✅ Real support email wired into Settings → Send Feedback: `feedback.stackup@undisputed-dev.com`.
- ✅ Leftover template art removed.
- ✅ **Upload keystore generated** at `C:\Users\Pape\keys\stackup-upload.keystore`
  (alias `stackup`). **Back this file up** — see the box in Step 6.
- ✅ Privacy policy authored: `docs/privacy-policy.html` (ready to host in Step 2).

You still have to: create the developer account, host the policy, capture screenshots, write
the listing, build the signed AAB, and upload. That's the rest of this guide.

---

## Step 1 — Create your Google Play developer account

☐ Go to **https://play.google.com/console** and sign in with the Google Workspace account
you want to own the app (e.g. your `@undisputed-dev.com` account).

☐ Choose an account type:

| | **Personal** | **Organization** (recommended for a company brand) |
|---|---|---|
| Verification | Government photo ID | Legal business name + a **D-U-N-S number** (free, can take days to obtain if you don't have one) |
| Shows on store as | Your chosen developer name | Your organization name |
| New-account testing rule | **Must run a closed test with ≥12 testers for ≥14 days** before you can publish to Production | **Not** subject to the 12-tester/14-day rule |

> Because "Undisputed-Dev" is a brand and the support email is on that domain, **Organization**
> is the cleaner choice and it **skips the 12-tester/14-day closed-testing requirement** that
> personal accounts now face. If you don't have a D-U-N-S number, you can request one for free
> during signup (Google links to the request form) — but it adds days, so decide early.

☐ Pay the **one-time US$25** registration fee.

☐ Fill in the developer profile: developer name (shown publicly), contact email
(`feedback.stackup@undisputed-dev.com`), phone, address.

☐ Complete identity/D-U-N-S verification. **Wait for the "verified" email** before you can
publish (you can still set everything else up meanwhile).

---

## Step 2 — Host the privacy policy (GitHub Pages)

Google requires a **public URL** for your privacy policy. We'll serve `docs/privacy-policy.html`
from GitHub Pages, free.

**If this repo is (or can be made) public:**

☐ Push the repo to GitHub if it isn't already.
☐ On GitHub: **Settings → Pages**.
☐ Under "Build and deployment", Source = **Deploy from a branch**; Branch = **main**,
   Folder = **/docs**. Save.
☐ Wait ~1 minute. Your policy is now at:
   `https://<your-github-username>.github.io/<repo-name>/privacy-policy.html`
☐ Open that URL in an **incognito window** to confirm it loads publicly. Copy it — you'll
   paste it into the Play Console in Step 4.

**If you want to keep this repo private:** create a new *public* repo (e.g. `stackup-legal`),
copy `docs/privacy-policy.html` into its `/docs` folder (or root), and enable Pages the same way.

> Prefer a Google Site instead? Create a page in Google Sites, paste the text from
> `docs/privacy-policy.md`, publish, and use that URL. GitHub Pages is simpler and versioned.

---

## Step 3 — Create the app entry in Play Console

☐ Play Console → **All apps → Create app**.
☐ App name: **StackUp – Simple Gym Tracker** (this is the store title; ≤30 chars — it fits).
☐ Default language: **English (United States)** (add German later if you localize).
☐ App or game: **App**.
☐ Free or paid: **Free** (this is permanent for free apps — you can't later charge for the
   same listing).
☐ Tick the Developer Program Policies + US export law declarations. **Create app.**

---

## Step 4 — Fill in the required policy & content declarations

In the left nav, work through **Policy → App content** (and Store settings). Play won't let
you publish until every item here is green. Recommended answers for StackUp (local-only, no
ads, no data collection):

☐ **Privacy policy** → paste the URL from Step 2.

☐ **Ads** → **No, my app does not contain ads.**

☐ **App access** → **All functionality is available without special access** (no login).

☐ **Content rating** → start the questionnaire. Category: *Utility / Productivity*. Answer
   **No** to all violence/sex/drugs/gambling questions. Result: **Everyone / PEGI 3**.

☐ **Target audience and content** → target age group **18+** (or 13+). Since it's not aimed
   at children, answer that it is **not** primarily child-directed. This avoids the Families
   policy requirements.

☐ **Data safety** → this is the important one. Answer:
   - Does your app collect or share any user data? → **No.**
   - (If asked about data types) select **none**.
   - Is all data encrypted in transit? → N/A (no data leaves the device).
   - Do you provide a way to request data deletion? → Not required (no data collected); you
     can note that uninstalling removes all local data.
   - **Note on permissions:** the app declares `INTERNET` / `ACCESS_NETWORK_STATE`. These are
     for the system in-app-review prompt and platform diagnostics only — **no user data is
     collected or transmitted**, so the honest Data Safety answer remains "no data collected."

☐ **Government apps** → No. **Financial features** → No. **Health apps** → No (it's fitness
   logging, not health/medical data). **COVID-19** → No.

☐ **News app** → No.

---

## Step 5 — Store listing (copy + graphics)

Left nav → **Grow → Store presence → Main store listing.**

### Ready-to-paste text

**App name** (≤30 chars):
```
StackUp – Simple Gym Tracker
```

**Short description** (≤80 chars):
```
Log your lifts. It marks each set up/down/keep and suggests next session's weight.
```

**Full description** (≤4000 chars):
```
StackUp is a minimal, no-nonsense gym tracker that replaces the paper notebook. Log your
working weight and reps per exercise — StackUp does the thinking about what to lift next.

THE KEY IDEA: AUTO-PROGRESSION
After each set you enter the reps you managed. StackUp compares them to your target rep range
and marks every exercise:
  ▲ UP — you beat the range, so it suggests adding weight next session
  ▬ KEEP — you're in range, hold the weight
  ▼ DOWN — you fell short, so it eases the weight back
Next time you train that split, your suggested weights are already adjusted. No spreadsheets,
no guesswork.

BUILT FOR THE GYM FLOOR
• Start a session for a split (Upper/Lower, Push/Pull/Legs, Full Body, or your own) with last
  time's weights pre-loaded.
• Everything saves the instant you type — no Save button. Get a phone call mid-set and close
  the app? Your session is exactly where you left it.
• Tap any mark to override it by hand.

FEATURES
• Ready-made splits and a 20-exercise library with movement icons — fully customizable
• Weight-based (machines, barbell) and rep-based (bodyweight) tracking
• Global defaults plus per-exercise overrides for rep range, weight increment, and how sets count
• Progression charts per exercise, over 3 months / 1 year / all time
• kg or lbs display

PRIVATE BY DESIGN
• No account, no sign-in
• No ads, no tracking, no analytics
• Works fully offline — your data stays on your device
• Free

Simple and mature: no rest timers, no social feed, no gamification. Just an honest log that
tells you what to lift next.
```

> Char counts: the short description above is 79/80. If you edit it, keep it ≤80.

### Graphics assets (exact sizes Google requires)

| Asset | Size / format | Notes |
|---|---|---|
| **App icon** | 512 × 512 px, 32-bit PNG | Export from the teal StackUp icon (`Resources/AppIcon/`). See tip below. |
| **Feature graphic** | 1024 × 500 px, PNG/JPG (no alpha) | Banner at top of the listing. Simple: teal `#2E6E62` background, "StackUp" + tagline, the ▲▬▼ marks. |
| **Phone screenshots** | 2–8 images, PNG/JPG; min 320 px, max 3840 px on any side; 16:9 or 9:16 | Capture in Step 6. |
| (optional) 7"/10" tablet shots | same rules at tablet res | Skip for a phone-first launch. |

**Tip — exporting the 512px icon:** open `Resources/AppIcon/appicon.svg` (with
`appiconfg.svg` foreground on the `#2E6E62` background) in Inkscape/any SVG tool and export a
512×512 PNG. Or screenshot the installed app's launcher icon at high res. The feature graphic
can be made in any image editor / Canva using the same teal and the arrow-mark motif.

☐ Also set: **App category** = *Health & Fitness*; **Tags**; **Contact details** (email
`feedback.stackup@undisputed-dev.com`, plus the privacy-policy URL/website if you have one).

### Capturing the phone screenshots

Play needs **phone** screenshots, so shoot them on the Android emulator (not the Windows
dev-head). Populate some good-looking history first so the charts and cards aren't empty.

☐ Boot the emulator:
```powershell
& "$env:LOCALAPPDATA\Android\Sdk\emulator\emulator.exe" -avd fmc_pixel
```
☐ Build & deploy the app to it (a Debug deploy is fine for screenshots):
```powershell
dotnet build src/StackUp.App -f net10.0-android -t:Run
```
☐ Add a few sessions with varied weights/reps so an exercise shows a rising progression
   chart and the ▲▬▼ marks appear (a few minutes of tapping through a couple of workouts is
   enough; the demo-seeder tool targets the Windows DB, so seed on the emulator by hand).
☐ Capture each screen (binary-safe pull — a PowerShell `>` redirect corrupts the PNG):
```powershell
adb shell screencap -p /sdcard/shot.png
adb pull /sdcard/shot.png .\screenshots\01-today.png
```
Repeat, renaming, for ~4–6 screens worth showing:
   1. Today / next-session card with suggested weights
   2. A session in progress with the ▲▬▼ marks visible
   3. The finish summary
   4. Stats — a progression chart
   5. The exercise/split library
☐ These PNGs are already at phone resolution and aspect ratio — upload them directly in the
   listing's Phone screenshots section.

---

## Step 6 — Build the signed release AAB

Run this on **this PC** (PowerShell). Substitute the keystore password you saved when the
keystore was generated.

```powershell
dotnet publish src/StackUp.App -f net10.0-android -c Release `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore="C:\Users\Pape\keys\stackup-upload.keystore" `
  -p:AndroidSigningKeyAlias=stackup `
  -p:AndroidSigningStorePass="YOUR_KEYSTORE_PASSWORD" `
  -p:AndroidSigningKeyPass="YOUR_KEYSTORE_PASSWORD"
```

The signed bundle is written to:
```
src\StackUp.App\bin\Release\net10.0-android\publish\com.stackupgym.app-Signed.aab
```
That `-Signed.aab` file is what you upload.

> ⚠️ **BACK UP THE KEYSTORE.** `C:\Users\Pape\keys\stackup-upload.keystore` and its password
> are the only way to ship updates to this listing. Copy both to a password manager / secure
> backup **now**. (Play App Signing — Step 7 — gives a recovery path, but don't rely on it.)

---

## Step 7 — Upload to Internal testing, then promote

We publish to the **Internal testing** track first: near-instant availability, installable on
your own phone, no public exposure. Then promote to Production.

☐ Play Console → your app → **Testing → Internal testing → Create new release.**

☐ **Play App Signing:** on your first upload Google offers to manage the app signing key —
   **accept it (recommended).** Your `stackup-upload.keystore` is then the *upload key* (used
   to authenticate your uploads); Google holds the real *app signing key* that end-users' apps
   are signed with. Two different keys — this is normal and is the safety net if your upload
   key is ever lost.

☐ **Upload** `com.stackupgym.app-Signed.aab`.

☐ Release name auto-fills (e.g. `1 (1.0)`). Add short release notes ("First release").

☐ **Save → Review release → Start rollout to Internal testing.**

☐ **Testers:** Internal testing → Testers tab → create an email list containing your own
   Google account (and any friends). Save. Copy the **opt-in URL**.

☐ On your Android phone, open the opt-in URL, accept, then install StackUp from the Play link.
   **Smoke-test:** launch, run a session, confirm the ▲▬▼ marks and next-session suggestion
   work, and that Settings → Send Feedback opens a mail draft to
   `feedback.stackup@undisputed-dev.com`.

**When you're happy, go to Production:**

☐ **Production → Create new release** (or **Promote** the internal release).
☐ Confirm the AAB, add production release notes, **Save → Review → Start rollout to Production.**
☐ Submit. First-time review typically takes from a few hours up to a few days. You'll get an
   email when it's live.

> If you chose a **Personal** account in Step 1, the Production option is gated behind the
> **closed test with ≥12 testers for 14 days** rule — do that closed test first, then Production
> unlocks. **Organization** accounts skip this.

---

## Step 8 — Publishing updates later (keep this handy)

Every new upload must have a **higher versionCode**. Before each release build:

1. In `src/StackUp.App/StackUp.App.csproj`, bump **`<ApplicationVersion>`** (versionCode) by 1
   — Play rejects a duplicate. Update **`<ApplicationDisplayVersion>`** (the user-visible
   "1.0", "1.1", …) when it's a meaningful release.
2. Rebuild the signed AAB (Step 6) with the **same keystore**.
3. Upload to a track and roll out.

Keep the keystore + password backed up forever. Keep the ApplicationId `com.stackupgym.app`
unchanged — it's permanent for this listing.

---

## Quick reference — the whole flow

1. **Create developer account** (Organization, $25, verify) ← start immediately
2. **Host privacy policy** on GitHub Pages → get URL
3. **Create app** in Play Console
4. **App content** declarations → all green (Data safety = no data collected)
5. **Store listing** → paste text (Step 5), upload icon 512², feature graphic 1024×500, screenshots
6. **Build signed AAB** → `dotnet publish … -c Release` → `com.stackupgym.app-Signed.aab`
7. **Internal testing** upload → test on phone → **promote to Production** → submit
8. **Updates:** bump `ApplicationVersion`, rebuild with same keystore, re-upload
