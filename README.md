# Unity.Rea.Incognito

A Unity 6 rebuild of *Incognito*, a 2013 GameMaker point-and-click stealth prototype: a
Shadowrun-flavored cyberpunk street, a camera held deliberately close instead of a wide adventure-
game establishing shot, and a mouse cursor that doubles as the player character's own tracking
eyeball.

[![Unity](https://img.shields.io/badge/Unity-6000.4.3f1-black)](ProjectSettings/ProjectVersion.txt) [![URP](https://img.shields.io/badge/URP-17.4-2b6cb0)](Packages/manifest.json) [![C#](https://img.shields.io/badge/language-C%23-239120)](Assets/Rea.Incognito) [![Core tests](https://img.shields.io/badge/core%20tests-247%20passing-brightgreen)](Tests/Core.Tests) [![Platform](https://img.shields.io/badge/platform-Windows-0078D6)](tools/build.ps1) [![Status](https://img.shields.io/badge/status-feature%20complete-yellow)](docs/USER_STORIES.md)

Every room and mechanic of the original is rebuilt: the title, Bleecher Street (street00), 2nd
Avenue (street01) and the shop, with the original's art, sounds, music and fonts. There is no
public build; build it from source with the Quick start below.

## Why

The original prototype (GameMaker 8, 2013) tried a few things worth rebuilding properly:

- **An intimate camera.** Instead of centering the player, the view pins near the screen edge
  opposite the player's facing direction, so there's always room ahead and the frame feels close
  rather than panoramic.
- **A cursor that's also a character.** The player's eyeball tracks the mouse with a damped
  parallax offset and blinks on its own, and the cursor itself is drawn as an eye icon that
  changes with context (look / talk / pointer / aim).
- **Hand-rolled "sinewave" movement.** A triangle-wave bob on every walk cycle and arm swing:
  not a static walk pose, and not literal trig, but it reads as alive.
- **Social stealth.** No cover mechanic: NPC suspicion rises from proximity and mutual eye
  contact, and falls when you look away.

The rebuild keeps the gameplay exactly and rebuilds the code: an engine-free, tested Core, content
and tuning as validated data, rooms composed in code, and modern Unity (URP, the Input System,
UI Toolkit). Where the original was plainly broken, the rebuild plays the way it was meant to, and
[`docs/BIBLE.md`](docs/BIBLE.md) records each fix.

## Features

- **Locomotion.** Walk with A/D or the arrow keys, sprint with Shift (ramping to double speed),
  triangle-wave walk bob, footsteps on each bob peak, a whoosh on turning around.
- **Close-framing camera.** Leads the player in the facing direction, swings over on a turn,
  pans vertically with the mouse, clamped inside the room.
- **Eyeball and eye cursor.** Damped-parallax pupil, randomized blink, cursor sprite per mode.
- **Look and talk.** The mouse wheel (or a right click with the gun holstered) toggles look and
  talk. Look types an NPC's or prop's description at the click; talk walks the NPC over to you.
- **NPC patrol and suspicion.** Data-driven waypoint routes, the same bob as the player, and a
  suspicion percentage driven by distance, mutual facing and whether you're looking down.
- **Weapon.** Z (or the HUD pistol icon) draws and holsters, the right button aims with the arm
  pivoting toward the cursor, the left button fires a bullet that shoves the NPC it hits.
- **Hacking overlay and typed console.** Q toggles the console view (pointer cursor, circuit
  backdrop, edge-scrolling camera); a pointer click on an NPC hacks it and opens the typed console
  (`help`, `list`, `list sheep`/`camera`/`computer`/`all`, `reboot <id>`, `overwrite <id>`,
  `clear`).
- **Rooms.** Doors (W within reach), side exits with autowalk and a fade, entry placement by the
  side you came from, rain, flickering lampposts, clickable props, the intro poem, each room's
  music.
- **Display.** The original's 1920x1080 view, letterboxed on any other aspect, fullscreen from the
  title; F1 toggles fullscreen, F12 shows a debug readout, Escape closes the console or quits.
- **Online-ready.** Beside the untouched local game, an online scene on the shared
  [Unity.Multiplayer](https://github.com/mindattic/Unity.Multiplayer) kit: Host, Join by IP, Join by
  code, a lobby with the roster, reconnect and an Esc menu; every player walks and draws on one street,
  run by one server authority through the same Core commands local play sends (BIBLE, U.INC-T43).

## Quick start

Requirements: Windows, Unity **6000.4.3f1** (Unity Hub's default install path), the .NET SDK (for
the Core tests), and [Unity.Shared](https://github.com/mindattic/Unity.Shared),
[MindAttic.Capture](https://github.com/mindattic/MindAttic.Capture),
[Unity.Multiplayer](https://github.com/mindattic/Unity.Multiplayer) and
[MindAttic.Components](https://github.com/mindattic/MindAttic.Components) cloned beside this repo,
which `Packages/manifest.json` references as local `file:` packages.

- **Play online:** `powershell -File play.ps1 -Online` (or launch `Incognito.exe -online`) opens the
  online scene's menu instead of the title; in the Editor, open `Assets/Rea.Incognito/Scenes/Online.unity`
  and press Play. `Incognito/Build Online Scene` regenerates it. A dedicated server and smoke-test bots
  run it from `mp-host.json` (`mp-host game local`, `mp-host game smoke`).

- **Play in the Editor:** open the project in Unity 6000.4.3f1, open
  `Assets/Rea.Incognito/Scenes/Rooms/Titlescreen.unity` and press Play. Rooms rebuild their content
  from data at Awake, so a room scene looks bare until you press Play.
- **Regenerate the scenes:** `Incognito/Build Room Scenes` in the Editor menu rewrites the four
  room scenes and the build settings (`Assets/Rea.Incognito/Editor/RoomSceneBuilder.cs`).
- **Build:** `powershell -File tools/build.ps1` writes `Builds/Windows/Incognito.exe` in batch mode.
- **Test:** `powershell -File tools/test.ps1` runs the Core, EditMode and PlayMode suites;
  `powershell -File run-core-tests.ps1` runs only the engine-free Core tests with `dotnet test`.
- **Parity tour:** `powershell -File tools/verify-parity.ps1` (Editor closed) plays the whole game
  with scripted input, muted, and writes screenshots to `Logs/parity-tour/`.
- **Launch a playable build for development:** `powershell -File play.ps1` (builds it first if
  `Builds/Windows/Incognito.exe` is missing; `-Build` rebuilds, `-Wait` waits for it to exit, the log
  goes to `Logs/play.log`). This is not `tools/build.ps1`/`tools/test.ps1`'s `-batchmode` runs: it
  starts a real, on-screen player.

The batch scripts (`tools/unity.ps1` underneath) need the Editor closed on this project, and keep
each run's Unity log in `Logs/`.

### Headless launches

`powershell -File play.ps1 -Headless` runs the built player with no window for scripted testing
(U.INC-T54): it goes through `..\Unity.Shared\tools\Start-Headless.ps1` (`--headless -batchmode
-nographics`), skips the title straight into Bleecher Street, and waits for the
`MINDATTIC_HEADLESS_READY Incognito` line in `Logs/play.log`. The player keeps running; stop it
yourself. Without the flag nothing changes. See Unity.Shared's README, "Headless mode".

### Muted launches

`play.ps1` launches muted: it sets `MINDATTIC_CLI_MUTED=1` and passes `-muted`, so a CLI or agent
starting the game never blasts audio. Pass `-Sound` to launch with audio. Either switch works on its
own when you start `Incognito.exe` (or the editor) yourself:

```
$env:MINDATTIC_CLI_MUTED = '1'; .\Builds\Windows\Incognito.exe
.\Builds\Windows\Incognito.exe -muted
```

The mute is the shared `com.mindattic.unity.shared` session mute: read before the first scene loads,
it makes the player profile's settings report music and sound effects muted at volume 0 for that run
only, and `Incognito.Audio.SessionAudio` silences the `AudioListener`. The player's saved
`settings.json` is never written, so the next normal launch has sound. Because Incognito has both
music (`MusicPlayer`) and sound effects (footsteps, the HUD flick, the weapon), `SessionAudio` only
silences the listener once *both* channels report muted, so muting just one of your own profile's
channels never silences the other. `tools/build.ps1`/`tools/test.ps1`'s `-batchmode` runs play nothing
audible and don't need it.

## How it works

| Assembly | Folder | What it holds |
|---|---|---|
| `Rea.Incognito.Core` | `Assets/Rea.Incognito/Core` | Pure C# (`noEngineReferences`): `GameWorld` and its commands/events, every `*State` rule class, `SuspicionMeter`, `CommandInterpreter`, `ScreenRatio`, `GmText`, the settings registry and content catalog. References only Unity.Shared.Core. |
| `Rea.Incognito.Runtime` | `Assets/Rea.Incognito/Scripts` | MonoBehaviours that read input, submit commands, and present state and events: player, NPCs, weapon, eye, camera, hacking overlay, console, rooms, audio, the UI Toolkit HUD (`HudRoot`), `GameDisplay`. |
| `Rea.Incognito.Editor` | `Assets/Rea.Incognito/Editor` | Scene and build tooling: `RoomSceneBuilder`, `SandboxSceneBuilder`, `OriginalArt`, `PlayerBuilder`. |
| `Rea.Incognito.Online` | `Assets/Rea.Incognito/Online` | Online play on Unity.Multiplayer: `IncognitoNetPlayer` (a `NetEntity` per seat), `IncognitoAuthority` (the server's one `GameWorld`), `IncognitoOnlineGame`, `IncognitoBot`, `OnlineLaunch`. `Rea.Incognito.Editor.Online` writes the online scene (`OnlineSceneBuilder`). |
| Tests | `Tests/Core.Tests`, `Assets/Rea.Incognito/Tests/{EditMode,PlayMode}` | `dotnet test` for Core; Unity EditMode for the MonoBehaviour layer; PlayMode for room traversal, the HUD and text. |

- **Commands in, events out.** The game changes only through `GameWorld.Submit(GameCommand)`;
  every command carries a `PlayerId`, and presentation plays the resulting `GameEvent`s. There is
  no global current-player singleton.
- **Content and tuning are data.** `Assets/Rea.Incognito/Resources/Data/game-content.json` (rooms,
  NPCs, descriptions, the poem, console copy, the art, sound and font catalogs) and
  `game-tuning.json` (locomotion, camera, suspicion, weapon, rain, display) are validated on load
  by `GameSettings`.
- **Built in code.** Each committed room scene holds only the player rig and a `RoomBootstrap`,
  which rebuilds the room from its data at Awake, so a scene can't drift from its data.
- **Input** is a code-built Input System reader (`GameInput`) producing an engine-free
  `InputSnapshot`. **Rendering** is URP's 2D renderer, with the original's prop depths as sorting
  layers; Core and gameplay never reference the pipeline.

## Status

Feature complete, and a **complete proof of concept** of the original: the final parity tour
(`ParityTourPlayTests`, run by `tools/verify-parity.ps1`, muted) plays the title, Bleecher Street,
2nd Avenue and the shop through a virtual keyboard and mouse, every row of the parity checklist in
[`docs/RECONSTRUCTION.md`](docs/RECONSTRUCTION.md) section 11 is ticked, and `ParityChecklistTests`
keeps it honest. Every live piece of original art is drawn, down to the console view's
brain-select overlay, the GUI arrow cursor and the dashboard hover backdrop (U.INC-T39).

## Limitations

- Windows only; the original shipped only a Windows build.
- All three original fonts draw as Arial: High Tower Text and Lucida Console can't be shipped.
- What 100% suspicion does is undecided; the original never wired a consequence.

## Documentation

- [`docs/RECONSTRUCTION.md`](docs/RECONSTRUCTION.md): the reverse-engineering record, mechanic by
  mechanic, read out of the original `.gml`/`.gmx` source.
- [`docs/BIBLE.md`](docs/BIBLE.md): the rebuild's brief, its systems and every decision where it
  differs from the original, including the original content deliberately not ported.
- [`docs/USER_STORIES.md`](docs/USER_STORIES.md): stories and the ticket log, generated from the
  ticket database.
- [`AGENTS.md`](AGENTS.md): the rules every contributor and agent follows here.

## Original source

`R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\_PRE_2017\gmlIncognito\09-05-13_2.19am`: read-only
reference, never edited by this project.
