# Escape: The Last Exit (MonoGame / top-down)

A 10-level top-down escape/adventure game built on your existing MonoGame
"Escape" project, using the dungeon + facility art pack you provided.

**Author:** Abdullah
**Roll No.:** _(add your roll number here)_

---

## What changed from the project you uploaded

The `Escape` project was still running MonoGame's default **platformer demo**
(a side-scrolling jump-and-run with monsters, gems, and text-based tile
levels). That has been removed:

- Deleted: `Game/Level.cs`, `Tile.cs`, `TileCollision.cs`, `Layer.cs`,
  `PlayerMode.cs`, `Gem.cs`, `GemState.cs`, the old `Player.cs`/`Enemy.cs`,
  and their content (parallax backgrounds, monster/player sprites, `.txt`
  level maps, jump/fall sound effects, "you win/lose" banners).
- Kept: the reusable engine pieces — `ScreenManager`, the menu system
  (`MenuScreen`/`MenuEntry`), `Animation`/`AnimationPlayer`, particle
  effects, input handling, localization, and the settings-storage system.

In their place is a new **top-down** game (WASD/arrow movement, camera
follows the player) built from your uploaded art.

## Your art, wired in

Your `Untitled_design.zip` was a genuinely complete top-down dungeon +
sci-fi facility tileset. The gameplay-critical pieces are cropped, resized,
and imported into the MonoGame content pipeline (`Content/Escape.mgcb`):

- **Doors** — normal, locked, exit (`Content/Doors/`)
- **Tiles** — a dungeon floor/wall set and a facility floor/wall set
  (`Content/Tiles/`), used depending on each level's theme
- **Traps** — spike, laser, fire/lava, moving block (`Content/Traps/`)
- **Items** — 4 colored keys, a health potion (`Content/Items/`)
- **HUD icons** — heart, hourglass (`Content/UI/`)
- **Main menu background** — the dungeon-gate artwork (`Content/Backgrounds/`)
- **Characters** — player + 2 enemy types (green goblin, red goblin), each
  with down/up/side-facing sprites (`Content/Characters/`)

The other ~110 images from your design file that weren't used for a specific
gameplay element are **not lost** — they're preserved, cropped, in the
`UnusedArt/` folder next to the project (outside the MonoGame content
pipeline, so they don't bloat the build). Drag any of them into
`Content/Escape.mgcb` via the MGCB Editor later if you want to use them for
extra decoration, a title-screen variant, etc.

## How to open and run it

1. Requires **Visual Studio 2022** (or newer) with the MonoGame templates /
   workload installed, and the MGCB Editor for content changes.
2. Open `Escape.sln` (or the folder) in Visual Studio.
3. Set **Escape.WindowsDX** (or **Escape.DesktopGL**) as the startup
   project and press **F5**.
4. `Escape.Android` / `Escape.iOS` share the same `Escape.Core` code and
   content, but I haven't set up mobile SDKs to test them here.

I don't have a Windows/MonoGame environment in this sandbox to compile-test
the build, so please give it a run in Visual Studio and send me any build
errors — I'll fix them fast.

## Building an APK for Android

### One-time setup

1. Open **Visual Studio Installer**.
2. Click **Modify** on your Visual Studio install.
3. Under **Workloads**, check **".NET Multi-platform App UI development"**
   (this brings in the Android SDK/tooling).
4. Click **Modify** and let it install.

### Build the APK

1. Open the solution in Visual Studio.
2. In Solution Explorer, right-click **Escape.Android** → **Set as Startup
   Project**.
3. Switch the configuration dropdown (top toolbar) from **Debug** to
   **Release**.
4. Right-click **Escape.Android** again → **Publish** (or **Archive**).
5. If it asks for a signing certificate, choose **Create New** — that's
   fine for a testing/sideload build.
6. Click **Publish** / **Create** and wait for the build to finish.
7. The `.apk` file will be in:
   ```
   Escape.Android/bin/Release/net9.0-android/publish/
   ```

Or from the command line, from inside `Escape.Android/`:
```
dotnet publish -f net9.0-android -c Release
```

### Quickest option — test directly on your own phone

1. On the phone: **Settings → Developer Options → USB Debugging** (if
   Developer Options isn't visible, go to **Settings → About Phone** and
   tap **Build Number** 7 times to unlock it).
2. Connect the phone to your PC by USB.
3. In Visual Studio, pick your phone from the device dropdown (top
   toolbar) and press **F5** — it builds, installs, and launches on the
   phone directly.
4. That build's `.apk` lands in `Escape.Android/bin/Debug/net9.0-android/`.

### Before publishing anywhere public

`Escape.Android.csproj` still has the placeholder `ApplicationId`
`com.companyname.Escape` — change it to something unique
(e.g. `com.abdullah.escapethelastexit`) before distributing the APK or
submitting to the Play Store. Publishing to the Play Store also needs an
`.aab` (not `.apk`) and a proper signing keystore:
```
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=myapp.keystore -p:AndroidSigningKeyAlias=myapp -p:AndroidSigningKeyPass=xxxx -p:AndroidSigningStorePass=xxxx
```
That step is only needed if/when you actually publish — for testing and
sideloading, the steps above are all you need.

## Controls

| Key | Action |
|---|---|
| W / Up | Move up |
| S / Down | Move down |
| A / Left | Move left |
| D / Right | Move right |
| Esc | Pause |

## What's implemented

- **Main Menu → Level Select → Play → Level Complete / Game Over → next
  level or retry**, matching the original spec's flow. Level Select shows
  each level as Locked / unlocked / Completed.
- **10 procedurally-generated levels** (`Game/LevelBuilder.cs`): each is a
  chain of rooms connected by doorways. Keys are always placed in a room
  before the locked door that needs them, so every level is guaranteed
  solvable. Room count, key count, trap count/types, and enemy
  count/speed all scale up with the level number, and each level keeps the
  name/objective/theme from the original design doc (Tutorial Escape →
  Ultimate Escape).
- **Player**: top-down 4-direction movement and animation, wall collision,
  health with brief hit-invulnerability, a key inventory.
- **Enemies**: patrol between two points, detect the player within a
  range, chase, and return to patrolling — two enemy types (green/red
  goblin) alternate through the levels.
- **Traps**: static spikes, back-and-forth moving blocks, a timed laser
  (learnable on/off cycle), and fire/lava zones (from level 3 onward, with
  laser/fire appearing on later levels).
- **Keys & doors**: 4 colors, locked doors that only open with the
  matching key plus an on-screen "You need the Red Key!" prompt, and an
  exit door that only opens once the level's key requirement is met.
- **HUD**: level name, health bar, keys collected, objective text, and a
  countdown timer; a **mini-map** in the corner shows the player, enemies,
  remaining items, and the exit.
- **Level 7 ("Dark Escape")** uses a fog overlay that only lights a small
  area around the player.
- **Score**: time bonus + health bonus + objective bonus, shown at the end
  of each level, with a running total.
- **Save progress**: highest unlocked level, completed levels, and best
  scores/times are saved to a small JSON file
  (`%AppData%/Escape/progress.json` on desktop) via the project's existing
  settings-storage system, on its own file so it doesn't collide with your
  regular settings save.
- **Sound**: reused three of your existing sound effects for their new
  purpose (key/item pickup, taking damage, level complete), plus the
  existing background music track.

## Project structure (new/changed files)

```
Escape.Core/
  EscapeGame.cs                    (added progress-save wiring)
  Game/
    Direction.cs                    Up/Down/Left/Right facing enum
    PlayerCharacter.cs              player movement, health, keys, animation
    EnemyCharacter.cs               patrol/detect/chase AI
    DoorObj.cs / TrapObj.cs / ItemPickup.cs
    RoomLevel.cs                    everything describing one generated level
    LevelBuilder.cs                 builds all 10 levels (room-chain generator)
    TopDownLevelRuntime.cs          movement/collision/HUD/minimap/camera/timer
    Animation.cs / AnimationPlayer.cs / Circle.cs / RectangleExtensions.cs  (kept, unchanged)
  Screens/
    GameplayScreen.cs               (rewritten) hosts one playthrough
    LevelSelectScreen.cs             (new) menu list of 10 levels
    MainMenuScreen.cs                (rewritten) simple entry menu
    BackgroundScreen.cs              (one-line change) new menu art
    MenuScreen.cs / MenuEntry.cs / PauseScreen.cs / MessageBoxScreen.cs /
    SettingsScreen.cs / AboutScreen.cs / LoadingScreen.cs / GameScreen.cs  (kept, unchanged)
  Settings/
    GameProgress.cs                  (new) save-data shape
    EscapeSettings.cs / EscapeLeaderboard.cs / SettingsManager.cs / ...  (kept, unchanged)
```

## Extending it further

- **Tune a level's difficulty**: everything about a level (room count,
  keys needed, trap count, enemy count/speed, timer, theme, dark/light) is
  in `LevelBuilder.GetConfig(int n)` — change the numbers there.
- **Use more of your art**: pull anything from `UnusedArt/` back into
  `Content/`, add an `#begin ... #build` block to `Escape.mgcb` (or use
  the MGCB Editor), and reference it from `TopDownLevelRuntime.LoadContent`.
- **Walk-cycle animation**: right now each character direction is a single
  still frame. If you want real walking animation later, stitch 2–4 frames
  per direction into one horizontal strip image (frame width = image
  height) and `Animation`/`AnimationPlayer` will animate it automatically
  — no other code changes needed.
