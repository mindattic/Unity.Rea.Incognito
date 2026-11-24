---
codex: 1
project: Unity.Rea.Incognito
layer: bible
status: living
updated: 2026-10-24
---

# Incognito — rebuild bible

The brief for the Unity rebuild. Every mechanic here is grounded in docs/RECONSTRUCTION.md — read
that first for the "what the original actually did" evidence; this file is the "what we're
building" target, free to make GM8-era implementation details (hard-coded pixel offsets, stubbed
commands) cleaner without changing the feel.

## Pitch

A 2D side-on point-and-click stealth adventure. Shadowrun-flavored cyberpunk setting, Room 23
(Nintendo DS)-inspired pixel art, and a camera that stays deliberately close to the player instead
of the usual wide adventure-game establishing shot — the game's whole premise is that intimacy.

## Pillars

1. **Close camera, not a wide shot.** The view pins near the screen edge opposite the player's
   facing (room to walk into, not room already covered), pans vertically with the mouse. See
   RECONSTRUCTION §2.
2. **The cursor is the character.** The mouse isn't a disembodied pointer — the player (and every
   NPC) has a visible, parallax-tracking eyeball that reacts to where the player is looking, and
   the OS cursor itself is drawn as an eye icon. See RECONSTRUCTION §3.
3. **Stealth is social, not mechanical.** No cover system — suspicion rises from proximity and
   mutual eye contact, and falls when you look away or hide behind the console. See
   RECONSTRUCTION §4.
4. **Movement reads as alive.** A deliberate triangle-wave bob on every walk cycle and every arm
   swing — never a static idle pose while moving. See RECONSTRUCTION §1.

## Systems (status tracked per-story in docs/USER_STORIES.md)

Every system below is built. Core is `Assets/Rea.Incognito/Core` (engine-free), Runtime is
`Assets/Rea.Incognito/Scripts`, and the proving tests are in `Tests/Core.Tests` (dotnet test) and
`Assets/Rea.Incognito/Tests/{EditMode,PlayMode}`.

| System | Reconstruction ref | Code (Core / Runtime) | Ticket | Story | Proved by |
|---|---|---|---|---|---|
| Player locomotion (walk bob, sprint, facing) | §1 | `Player/PlayerLocomotionState`, `Motion/TriangleWaveBob` / `Player/PlayerLocomotion` | U.INC-T3 | US-A1 | `PlayerLocomotionStateTests` |
| Close-framing camera | §2 | `CameraRig/CloseFramingCameraState` / `CameraRig/CloseFramingCamera` | U.INC-T4 | US-A2 | `CloseFramingCameraStateTests` |
| Eyeball and eye cursor (parallax, blink, mode sprites) | §3 | `Eye/EyeballState` / `Eye/PlayerEyeball`, `Eye/EyeCursor` | U.INC-T5 | US-A3 | `EyeballStateTests` |
| Cursor mode cycling (look/talk) and descriptions | §3, §4 | `Typing/TypewriterState` / `Npc/NpcInteraction`, `Typing/DescriptionTypewriter` | U.INC-T6 | US-A4 | `CursorModeCyclingTests`, `TypewriterStateTests` |
| Hacking overlay (console view, pointer-click hack) | §5 | / `Hacking/HackingOverlay`, `Hacking/HackingSession` | U.INC-T7 | US-A5 | `HackingOverlayTests` |
| NPC patrol and suspicion meter | §4 | `Npc/NpcPatrolState`, `Npc/SuspicionMeter` / `Npc/NpcController` | U.INC-T8 | US-A6 | `NpcPatrolStateTests`, `SuspicionMeterTests` |
| Weapon equip/aim/fire, bullet and muzzle smoke | §1 | `Weapon/PlayerWeaponState`, `ArmSwingState`, `BulletState`, `MuzzleSmokeState` / `Weapon/PlayerWeapon`, `Bullet`, `MuzzleSmoke` | U.INC-T9, T41 | US-A7 | `PlayerWeaponStateTests`, `BulletStateTests`, `BulletTests`, `MuzzleSmokeStateTests`, `MuzzleSmokeTests` |
| Typed command console | §6 | `Terminal/CommandInterpreter`, `Vicinity` / `Terminal/CommandPrompt` | U.INC-T10 | US-A8 | `CommandInterpreterTests`, `TerminalRuntimeTests` |
| Rooms, props, exits, rain, music and sound | §7, §8 | `World/GameWorld`, `RoomExitRules`, `RoomFadeState`, `RainState` / `World/RoomBootstrap`, `RoomComposer`, `Audio/` | U.INC-T11, T26 | US-A9 | `RoomTraversalPlayTests`, `RoomSceneBuilderTests`, `RoomRuleTests` |
| Debug keys (Escape, F1, F12) | §9 | / `World/DebugHotkeys` | U.INC-T15 | — | `DebugHotkeysTests` |
| Commands and events, multiplayer seams | — | `World/GameWorld`, `GameCommand`, `GameEvent`, `Ids` | U.INC-T21 | — | `GameWorldTests` |
| Content and tuning as validated data | — | `Settings/SettingsRegistry`, `Content/GameContent` / `Settings/GameSettings` | U.INC-T22, T25 | — | `SettingsRegistryTests`, `GameContentTests`, `JsonParsingTests`, `FieldCoverageTests`, `GameSettingsTests` |
| Input System | — | `Controls/InputSnapshot` / `Controls/GameInput` | U.INC-T23 | — | `GameWorldTests` (Core reads controls as plain data); `GameInput` itself has no test |
| HUD pistol icon and GUI-layer clicks | §10 | / `Weapon/HudPistolIcon`, `Hacking/HackingSession` (`MouseGui`) | U.INC-T30 | US-A10 | `HudPistolIconPlayTests`, `HackingOverlayTests` |
| Sound catalog | §8, §10 | `Content/SoundDefinition` | U.INC-T28 | US-A11 | `OriginalAudioParityTests` |
| Fonts, outlined text, floating text | §10 | `Text/GmText`, `Text/FloatTextState`, `Content/GameFontDefinition` / `UI/OutlinedText`, `UI/FloatText`, `UI/HudRoot` | U.INC-T31 | US-A12 | `OriginalFontParityTests`, `GmTextTests`, `OriginalTextPlayTests` |
| Screen ratio, letterbox, fullscreen | §10 | `Display/ScreenRatio` / `Display/GameDisplay` | U.INC-T32 | US-A13 | `ScreenRatioTests`, `GameDisplayTests` |
| Art catalog | §10 | `Content/ArtDefinition` / Editor `OriginalArt` | U.INC-T29 | US-A14 | `OriginalArtParityTests` |
| Console-view NPC overlay, GUI arrow cursor, dashboard backdrop | §5, §10 | / `Npc/NpcController`, `Hacking/HackingSession` (`GuiCursor`, `DashboardMenu`), `Hacking/HackingOverlay`, `Art/Shaders/InvertBlend` | U.INC-T39 | US-A14 | `HackingOverlayTests`, `ParityTourPlayTests` |
| End-to-end parity run | all | — | `ParityTourPlayTests` + RECONSTRUCTION section 11 | US-A15 | `ParityChecklistTests` |

## Decisions beyond the original (not yet made — flag when a ticket hits these)

- What happens at 100% suspicion (the original never wired a consequence — RECONSTRUCTION, Open
  questions). The rebuild clamps the meter at 100% and does nothing more, as the original did
  nothing.

## Decisions made

- **Close camera view size / vertical pan (U.INC-T4).** The reference view is the original
  1920x1080 (orthographic size = 540 original px at the placeholder 1px = 1/32 unit scale); the
  horizontal framing reads the live view width from the camera (since U.INC-T32 the view is
  letterboxed, so that is always 1920). The original's `y = mouse_y` was a no-op
  (its room was exactly view-height), so the rebuild makes it a direct, instant, bounded pan: mouse
  height on screen maps linearly onto ±135 original px (one eighth of the view height) around the
  camera's resting height. See `Assets/Rea.Incognito/Core/CameraRig/CloseFramingCameraState.cs`.
- **Eyeball and eye cursor (U.INC-T5).** Two systems, as in the original: `PlayerEyeball` (obj_eye)
  and `EyeCursor` (obj_mouse), both under `Assets/Rea.Incognito/Scripts/Eye/`. The eye keeps obj_eye's
  numbers verbatim (socket -108/+93 px by facing, pupil `7 + dx/160`, `-3 + mouse_y/160`, aim snap
  -1/15, 20px lid at 2px/step, 1-in-161 blink roll). `mouse_y` is read as the cursor's height from
  the top of a 1080px screen — what it equalled in the original's view-tall room — so the T4
  vertical pan doesn't feed back into the pupil. The blink runs in whole 60Hz steps (accumulated
  from frame time) so its odds and lid speed don't change with frame rate. The eye stack renders
  behind the player sprite (obj_eye depth 1 vs. obj_walker depth 0) and shows through the art's
  eye socket; the sandbox placeholder player is therefore spr_player-sized (512x1024, origin
  256,168) with a socket hole. The cursor hides the OS pointer and is an in-world sprite at the
  original 32px size, drawn in front of everything. Original eye and cursor sprites are imported
  verbatim from the 2013 project (`Assets/Rea.Incognito/Art/Original/`) at their GM origins. Aiming is
  an `Aiming` flag on both components for U.INC-T9 to set; look/talk scrolling (U.INC-T6) and
  pointer mode (U.INC-T7) write `EyeCursor.Mode`.
- **Look/talk cursor modes (U.INC-T6).** The mouse wheel (either direction) toggles look/talk, as
  the ticket asks; the original actually toggled on a right press (RECONSTRUCTION §3), so a right
  click also toggles while the gun is holstered — with it out, right click only aims (the original
  flipped the mode under the reticle too). Pointer is never reached by toggling. A left click on an
  NPC acts by mode, as obj_npc did: look types its description (per-NPC data on `NpcInteraction`)
  at the click with obj_typewriter's timing and the original `spr_desc_border`; talk walks it over;
  pointer raises `NpcInteraction.PointerClicked` for U.INC-T7. Cleanups: clicks do nothing while
  aiming (that click is the trigger); the talker stands one body width from the player on the side
  it came from instead of walking through to the far side, then coasts to a stop and runs the
  original's unreachable stop-and-face block (it faces the player and turns the player to face it
  if the player is idle and not aiming); it resumes its route 200 steps after the player's last
  walking step whichever way they walk (the original used 100 when walking left); the description
  box fades with its text and is sized to the text rather than `string_width * 3`. See
  `Assets/Rea.Incognito/Scripts/Npc/NpcInteraction.cs`, `Assets/Rea.Incognito/Core/Npc/NpcPatrolState.cs`
  and `Assets/Rea.Incognito/{Core,Scripts}/Typing/`.
- **Typed console stubs (U.INC-T10).** `help`, `list`, `list sheep`, `clear` and "Command not
  recognized" are ported verbatim (`#` line breaks become `\n`). The original's stubs get small,
  consequence-free implementations: `list camera`/`list computer` follow `list sheep`'s pattern
  (`CAM#<id>`, `CMP#<id>`), `list all` prints every type present, and `reboot <id>`/`overwrite
  <id>` resolve a target by handle (`shp#0097`) or bare id and raise an event on its
  `ConsoleTarget` — what a reboot or overwrite actually *does* is left to whichever later ticket
  needs it. "Vicinity" means within one view width (1920 original px) of the player, the same
  outer range the suspicion cue uses; the original listed every NPC in the room. The hacking
  overlay (U.INC-T7) opens it; Escape closes it, and movement keys are ignored while it is open.
  See `Assets/Rea.Incognito/Scripts/Terminal/`.
- **NPC patrol routes (U.INC-T8).** The original had one off-street waypoint and never stopped on
  arrival. The rebuild gives each NPC a data route of X waypoints walked in order and looped,
  turning on arrival (`a_near_b` ±20px); a one-waypoint route walks there and coasts to a stop
  through the original's otherwise-unreached `"stopping"` friction. Speed (5px/step), arrive range
  and the 2-step start alarm are as the original. See `Assets/Rea.Incognito/Core/Npc/NpcPatrolState.cs`.
- **Suspicion meter (U.INC-T8).** Rates, ranges and the accelerating out-of-range decay are the
  original's, as `SuspicionTuning` data (since U.INC-T22, `Suspicion` in game-tuning.json). Three cleanups: suspicion is clamped to
  0–100% (the original went negative and past 100); "facing each other" needs each to face toward
  the other (the original also counted back to back); "looking down" is the cursor in the lower
  half of the screen (the original's `mouse_y > room_height/2`, which equalled the screen since
  room and view were the same height). The range bands use the live view width. What 100% *does*
  is still open (above). See `Assets/Rea.Incognito/Core/Npc/SuspicionMeter.cs`.
- **Weapon (U.INC-T9).** Ported as the original had it: Z draws/holsters, holding the right button
  aims (walking locked), the left button fires a bullet that travels and shoves an NPC 100px on a
  hit (`BulletTarget`). GM quirks cleaned up rather than ported: releasing aim within the 5-step
  raise cancels it (the original's alarm still snapped the arms into the aim pose); the sprint arm
  swing (6°/0.24) applies walking either direction, not only left; the idle relax lands exactly on 0
  instead of jittering. The 10px arm "recoil" in `shoot_bullet.gml` was a no-op and is not ported.
  Aim angle stays screen-relative (cursor height in the view), as the original's was. Bullets are
  swept per frame (linecast) so the 60px/step shot can't skip a thin target. The weapon sounds
  are the original assets, wired since U.INC-T11. The muzzle smoke (shoot_bullet.gml's
  `effect_create_below(ef_smoke, ..., 1, c_black)`) is a GameMaker built-in particle effect with no
  original script or asset, so `MuzzleSmokeState`/`MuzzleSmoke` reproduce its puff — a handful of
  small black particles that drift and rise from the muzzle and fade out — as a code-built effect
  (`PlayerWeapon.Fire` spawns one on every shot, no Inspector wiring needed); U.INC-T41 fixed the
  `muzzleEffect` hook that no builder ever assigned. `PlayerWeapon` sets the T5
  `Aiming` flag on `EyeCursor` (reticle) and `PlayerEyeball` (fixed gaze) every frame. The sandbox
  NPC carries a `BulletTarget`, and `NpcController` routes the shove into its patrol position. The
  arm and bullet placeholders carry the original sprites' sizes and origins, so pivots and the
  muzzle line up as they did in GM. See `Assets/Rea.Incognito/Scripts/Weapon/`.
- **Rooms, props and audio (U.INC-T11).** `Incognito/Build Room Scenes` (`RoomSceneBuilder`)
  writes `Scenes/Rooms/{Titlescreen,Street00,Street01,Shop}.unity` from a data table transcribed
  from the `.room.gmx` files, each on a copy of the PlayerSandbox rig (sandbox ground, markers and
  console targets stripped; the NPC kept only in street00), and puts them in the build with the
  title first. Original slices, props and sounds are imported verbatim (`Art/Original`,
  `Audio/Original`; legacy files, as the `_new` ones are missing). Ported as the original had it:
  the 1:1 tiled street slices (no parallax — there was none), the title's scrolling skyline,
  rain, lampposts with their flickering light and mailboxes in front of the player (each
  clickable for its description), the floating shop door (W within 300px, `sfx_door_open`),
  side exits with autowalk and the 0.03/step black fade, entry placement by `enter_from`, the
  room name top-left, the intro poem on entering street00, footsteps on each bob peak (one of 16 at
  random), the turn-around whoosh, weapon sounds and `sfx_type` on console keystrokes. Decisions
  beyond the original: **music actually plays** (the original's calls were commented out) — each
  room's `current_track`, looped, carried across rooms and switched only when the track changes;
  the random jukebox stays unported. The shop barrier is a real wall (the original's push was
  sign-flipped and stopped no one). Clicking a prop does nothing while aiming, as with NPCs. A
  fresh player rig per scene carries only its facing across a room change (the original's one
  persistent player also kept the gun state). Clouds and the unplaced `obj_lamplight01` /
  `obj_prop_back|front` are not ported (no art, never placed). Text is IMGUI, outlined, at
  fnt_large's size (since U.INC-T26/T31: UI Toolkit, in each original font's own size). See `Assets/Rea.Incognito/Scripts/World/` and `Scripts/Audio/`.

- **URP behind a pipeline-agnostic layer (U.INC-T24).** `com.unity.render-pipelines.universal`
  17.4.0 (the version Unity.SpaceGod and Unity.Hare also pin), set up as a 2D project: a
  `UniversalRenderPipelineAsset` (`Assets/Rea.Incognito/Settings/Incognito_URP.asset`) backed by the 2D
  renderer (`Incognito_Renderer2D.asset`, `Renderer2DData` — no 3D forward/deferred renderer), MSAA
  off and HDR/shadows off (nothing here casts a shadow or needs antialiasing against pixel art),
  assigned in both `GraphicsSettings` and every `QualitySettings` tier. The original's cheap
  front-prop/back-prop depth trick (RECONSTRUCTION.md §7) is now actual Unity sorting layers
  (`ProjectSettings/TagManager.asset`), back to front: `Background` (tiles, title/scrolling
  backdrops), `BackProps` (lamppost, its light, side/door exits), `Default` (player, NPCs, arms,
  bullet, eye stack — unchanged), `Effects` (rain), `FrontProps` (mailbox), `Cursor` (the eye
  cursor, frontmost of all, as before). `sortingOrder` still gives the fine-grained order the
  original's depths encoded; the layer is the coarse band a sprite can never cross out of
  regardless of its order. Core and gameplay code reference only `SpriteRenderer.sortingLayerName`
  (plain Unity, not URP) and never `UnityEngine.Rendering.Universal`; no asmdef gained a pipeline
  reference. See `Assets/Rea.Incognito/Editor/{Room,Sandbox}SceneBuilder.cs`,
  `Assets/Rea.Incognito/Scripts/Eye/EyeCursor.cs`, `Assets/Rea.Incognito/Scripts/World/RainEmitter.cs`.

- **Hacking overlay (U.INC-T7).** Ported from what the original actually wired (RECONSTRUCTION
  §5, re-read): Q toggles the console view, which puts the cursor in pointer mode, fades
  `spr_circuitwall1` in over the screen (0.025/step) and hands the camera to the cursor
  (edge-scrolling within 32px of a side at 10px/step). A pointer click on an NPC hacks it: the
  `spr_program_active` icon eases up at bottom-left, the typed console slides in on the original
  `spr_console_selected` panel at obj_console's layout with its "CPU: 100/100%" readout, and the
  camera centres on the NPC (following it as it walks). The dead `obj_stealth` "scanning"
  dashboard is not ported. Cleanups: Escape closes the console and ends the hack (the icon stays
  up, inactive, and the camera goes back to the cursor); clicking the icon reopens or closes the
  console — what obj_mouse_gui's commented-out code intended; Q is ignored while the console
  has the keyboard (it is a letter then); Q off also closes the console and sinks the icon (the
  original left both up) and restores the cursor mode in use before Q (the original always
  restored "look"); clicks on the icon or panel don't also click the NPC behind them. Q no longer
  opens the prompt by itself. A program-icon click plays obj_mouse_gui's `sfx_flick` (U.INC-T28). See
  `Assets/Rea.Incognito/Scripts/Hacking/`.

- **Rooms built from data at runtime, and IMGUI replaced with UI Toolkit (U.INC-T26).** The four
  committed room scenes (`Assets/Rea.Incognito/Scenes/Rooms/*.unity`) no longer bake a room's
  background slices, lampposts, mailboxes, rain or exits into the scene file: `RoomBootstrap`
  (Runtime, `Assets/Rea.Incognito/Scripts/World/RoomBootstrap.cs`) rebuilds all of it at Awake, every
  time the scene loads, from `GameSettings.Content.FindRoom(roomId)` — the same validated data the
  room/NPC/console catalog already used (U.INC-T22) — via `RoomComposer`, an engine-but-not-editor
  static class the editor's Incognito/Build Room Scenes also calls indirectly (it only resolves and
  hands over the few static art/audio references `RoomBootstrap` needs, as `RoomRuntimeAssets`; it
  never builds the room's actual layout itself any more). A scene's committed content is now just a
  copy of the sandbox player rig plus one "Room Bootstrap" object, so it can never silently drift
  from the data it describes — proven by `Tests/EditMode/RoomSceneBuilderTests.cs`, which reruns
  the builder and asserts the regenerated scenes match (content, not Unity's own per-session
  `fileID`s or its fileID-ordered serialization, both normalized away before comparing). Trade-off:
  `RoomBootstrap` runs at Awake, not `[ExecuteAlways]`, so opening a room scene in the Editor
  without pressing Play shows only the bare player rig; use Play mode, or the fully-authored
  sandbox scene, to look at a room's content without playing.

  The 7 OnGUI (IMGUI) classes — `Room`, `DoorExit`, `DescriptionTypewriter`, `CommandPrompt`,
  `HackingOverlay`, `RoomFade`, `DebugHotkeys` — now draw through `HudRoot`
  (`Assets/Rea.Incognito/Scripts/UI/HudRoot.cs`), one code-built `UIDocument`/`PanelSettings` whose
  reference resolution (1920x1080, matched by height; since U.INC-T32 fitted keeping its aspect,
  inside a 1920x1080 stage on the letterboxed picture) replaces every class's own
  `Screen.height / 1080f` scale math: elements are laid out in plain original pixels and the panel
  scales them. `OutlinedText` now styles a `Label`'s native `-unity-text-outline-*` instead of
  drawing eight offset copies by hand, and `CommandPrompt`'s input is a real `TextField` (native
  focus, caret and backspace) instead of `CommandPrompt` parsing `Event.current` itself — a
  deliberate cleanup, since the original's hand-rolled i-beam was exactly this kind of incidental
  GM-era implementation detail. World-anchored labels (`DoorExit`, `DescriptionTypewriter`) convert
  a world position to panel space through the camera's screen point (since U.INC-T32,
  `GameDisplay.ScreenToView`, the same mapping every HUD element's hit-testing uses).

- **Every original sound, as a data catalog (U.INC-T28).** game-content.json's `Sounds` lists
  every sound the rebuild imports — id (the `.sound.gmx` name), file in `Audio/Original`, the
  original's `<streamed>` flag (music and whispers stream, effects decompress on load) and where
  the original plays it — and `RoomSceneBuilder` loads every clip through it by id
  (`LoadSound`), so no sound file is named by a literal in code. Room `Music` and the new
  `TitleScreen` entry are validated against the catalog. Added: `sfx_flick`, played on a
  program-icon click as obj_mouse_gui does (obj_command_prompt's own flick call sits in a
  commented-out block, so it has no other player), and the `whispers` ambient, catalogued with
  `Wired: false` and as `TitleScreen.UnwiredTrack`, never played — titlescreen's obj_music names it
  only in `//current_track = whispers;`, so the title keeps obj_music's default
  music_oppressivegloom. That line is a choice the author commented out, not a bug, so it is not
  "fixed". `Tests/Core.Tests/OriginalAudioParityTests.cs` proves every sound a live original
  object or script plays is catalogued, wired and on disk, and every file on disk is catalogued.

- **Every original sprite and background, as a data catalog (U.INC-T29).** game-content.json's
  `Art` lists every image the rebuild imports — id (the `.sprite.gmx`/`.background.gmx` name),
  file under `Art/Original` (copied verbatim), size, GameMaker origin (`xorig`/`yorigin`, 0,0 for
  backgrounds), whether it imports compressed (painted world art) or uncompressed (pixel sprites,
  HUD), whether it tiles (only the title's `bg_city_black`) and where the original uses it — and
  `Editor/OriginalArt` imports every image through it by id, pivot at the GM origin, so neither
  `RoomSceneBuilder` nor `SandboxSceneBuilder` carries an origin or a file literal any more. Every
  room's background slice is validated against the catalog. The sweep added what live original
  objects and rooms use but the rebuild lacked: `spr_icon_pistol` and `_selected`
  (obj_icon_pistol, toggle_gun), `spr_console` (obj_console), `spr_exit_side` and `spr_barrier`
  (collision masks), `spr_brain_select` (drawn over the NPC while the console is on),
  `spr_cursor_arrow` (obj_mouse_gui's GUI cursor), `spr_stealthwall` (obj_gui_menu's dashboard
  hover backdrop), `spr_black` (obj_fade) and `bg_black` (street00's base layer; the camera already
  clears to the same black). `spr_command_prompt`/`_selected` and `spr_linewall` are imported too
  but catalogued `Live: false`: obj_command_prompt, obj_stealth and obj_fade_stealth are never
  created. Importing is not wiring: the HUD pistol is U.INC-T30, and the brain-select overlay, GUI
  arrow cursor and dashboard hover are drawn by U.INC-T39 (below). `Tests/Core.Tests/OriginalArtParityTests.cs`
  proves every image a live original object or room uses is catalogued at its GM origin and size
  and on disk, and (where the original project is on the machine) that all 88 sprites and 21
  backgrounds are accounted for as live, imported or dead, with the transcription and the image
  bytes diffed against the original.

- **HUD pistol icon and GUI-layer clicks (U.INC-T30).** `HudPistolIcon`
  (`Assets/Rea.Incognito/Scripts/Weapon/HudPistolIcon.cs`) ports obj_icon_pistol: a 96x96 GUI icon
  40px in from the right edge (start_game.gml's `room_width - sprite_get_width(spr_icon_pistol) -
  40` on the 1920px view) that eases up from below the screen to `room_height - sprite_height`
  (984) with obj_program's own step code (`ProgramIconSlide`), drawn above the room fade (its
  depth -101 is one above obj_fade's -100: HudRoot's new `HudIcons` layer). It shows
  `spr_icon_pistol_selected` while the gun is out and `spr_icon_pistol` once holstered, read from
  the player's weapon state in the world, and a left click on it sends `ToggleGun`, the same
  command Z sends, so toggle_gun's sounds and arm poses follow. **Fixed defect:** the original
  created the icon at `y = room_height` and only obj_gui_menu's (and the never-created
  obj_stealth's) dead "scanning" branch ever set it "onscreen", so it never rose and
  obj_mouse_gui's pistol click was unreachable; the rebuild raises it when play starts and, as
  the persistent GM instance would, keeps it up in later rooms. obj_mouse_gui's click chain is
  `MouseGui.Resolve` — program icon, else console, else pistol icon — and a program icon or
  console click plays `sfx_flick`; a console click selects that console (`selected_id`), which in
  the rebuild's one-console HUD gives its input line the keyboard focus back. A click on the
  pistol icon counts as GUI, so it never also looks at or hacks an NPC behind it; firing was never
  GUI-gated in the original, so a click on it while aiming still fires, then holsters.

- **The console view's NPC overlay, the GUI arrow cursor and the dashboard backdrop (U.INC-T39).**
  The three live pieces of art T29 imported are now drawn as the original drew them.
  `NpcController` ports obj_npc's draw while `inverted` (the console view): after `draw_self()` it
  draws itself again under `draw_set_blend_mode_ext(bm_inv_dest_color, bm_inv_src_color)` (a
  child sprite on the `Incognito/InvertBlend` shader, `Blend OneMinusDstColor OneMinusSrcColor`,
  premultiplied so transparent texels leave the scene alone), then `spr_brain_select` at its
  origin, unflipped; both sit on the NPC's own sorting layer and order a hair nearer the camera, so
  nothing sorts between them. The global `over_gui` is `HackingSession.OverGui`, ported as the
  original ran it, which is not a hover test: Q on sets it and Q off clears it, and every left
  press runs obj_mouse_gui's chain, where a console press sets it, a program or pistol icon press
  leaves it and a press on anything else (the world, an NPC) clears it. While it is set and the
  player is not aiming, `HackingOverlay` draws obj_mouse_gui's `spr_cursor_arrow` at the pointer
  (origin 47,32) on HudRoot's new frontmost `GuiCursor` layer (depth -5000) and the eye cursor is
  hidden (obj_mouse's `if (!over_gui) draw_self()`). **Fixed defect:** with `over_gui` set and the
  player aiming, the original drew neither cursor, so the reticle vanished; the rebuild keeps the
  eye cursor (the reticle) up while aiming (`GuiCursor`). obj_gui_menu is `DashboardMenu`:
  `spr_stealthwall` (a black gradient, alpha 90 to 150) over the whole view on HudRoot's new
  `Dashboard` layer (depth -100, under the room fade), fading by obj_gui_console's 0.025/step;
  each step, only while the gun is holstered (`aimmode == "off"`) and the console backdrop is
  fully off, the mouse below `room_height - sprite_get_height(spr_program)` (994) fades it in and
  anywhere above fades it out; otherwise its mode is left as it was, as the original's was. The
  mouse is read in view pixels, which is the original's `mouse_y` because its view never moved
  vertically.

- **The original's fonts, outlined text and floating text (U.INC-T31).** game-content.json's
  `Fonts` catalogues the original's three fonts by their `.font.gmx` face and point size —
  fnt_basic (Arial 12pt), fnt_large (High Tower Text 16pt), fnt_system (Lucida Console 8pt) — and
  every HUD label takes its size from it (`GameFontDefinition`, `GameFonts`). GameMaker baked each
  font into a glyph atlas on the author's 120 DPI (125%) desktop, so the sizes the player actually
  saw are 20, 26.7 and 13.3 reference pixels, not the 96 DPI 16/21.3/10.7:
  `OriginalFontParityTests` proves the 120 DPI from the atlases themselves (fnt_basic's 23px Arial
  line, fnt_system's 8px Lucida Console advance) and diffs the catalog against `fonts/*.font.gmx`.
  **Deviation:** all three draw as Arial (the HUD panel's built-in LegacyRuntime.ttf); High Tower
  Text and Lucida Console are Microsoft-licensed system fonts the rebuild can't ship. Size,
  colour, outline and placement are the original's. `OutlinedText` (`Scripts/UI`) is the
  original's four draw calls as UI Toolkit styles: `Plain` (draw_text: obj_console's readout,
  output and input, fnt_system, unoutlined; its output keeps spaces, so the help text's indent no
  longer collapses), `Outline` (draw_text_outline, white over black: obj_room's name and obj_npc's
  "NN%" in fnt_large at 1px, obj_debug's F12 readout in fnt_basic at 2px, at obj_debug's 100,80),
  `OutlineAlpha` (draw_text_outline_alpha: obj_exit's door label and obj_typewriter's
  descriptions, fnt_large, 1px, faded as a whole) and `OutlineColor` (draw_text_outline_color).
  The NPC's suspicion label is now a UI Toolkit label like the rest (it was the last world-space
  `TextMesh`), centred (fa_center) with its top 200px above the NPC's origin. GM8's text
  conventions are `GmText` (Core): `#` is a line break, `##` a blank line, `\#` a literal hash
  (command_list.gml's `"SHP\#"`). game-content.json's display strings (room names, door labels,
  prop and NPC descriptions, the opening poem, the console's copy) are written in that original
  form and expanded once, when the content loads, so nothing downstream ever sees a GM `#`.
  show_float_text/show_float_text_color and obj_float_text are `FloatText` (`Scripts/UI`) over
  `FloatTextState` (Core): fnt_basic, centred, 1px outline, drawn 10px above its point, rising 1px
  and fading 0.007 a 60Hz step, destroyed on the step after its alpha runs out (144 steps). No
  live original object calls either script, so, as in the original, nothing shows one yet; the
  API is ported for the game's own use, not wired to an invented trigger.

- **Screen ratio, letterboxing and fullscreen (U.INC-T32).** obj_startup's (and obj_titlescreen's)
  "Setup Screen Ratio" sizes view 0's port to the window (`view_wport/hport =
  window_get_width/height()`) while the view stays 1920x1080, and the config's `option_scale = -1`
  ("keep aspect ratio") draws that port at the largest uniform scale that fits, centred, with
  black bars on the leftover axis. `ScreenRatio` (Core, `Assets/Rea.Incognito/Core/Display/`) is that
  maths, engine-free and tested in `Tests/Core.Tests/ScreenRatioTests.cs`: the fitted view rect in
  whole screen pixels, the camera viewport it makes, and screen-to-view (and back) point and rect
  mapping. `GameDisplay` (`Assets/Rea.Incognito/Scripts/Display/GameDisplay.cs`, installed before the
  first scene and kept across rooms) gives every screen camera that viewport each frame (a camera
  rendering into a texture keeps the whole texture), clears the bars to black with a camera that
  draws nothing, and is the one place screen points become view pixels: the HUD panel
  (`HudRoot`, now Shrink-fitted with every layer inside a centred, clipped 1920x1080 stage), the HUD
  click rects (program icon, console, pistol icon, which is now 40px in from the *view's* right
  edge, not the screen's, as start_game.gml placed it), and the pointer readings the eye, the gun's
  aim, "looking down" and the camera's vertical pan take — so on a 16:10, 4:3 or 21:9 screen the
  game shows exactly the 1920x1080 picture the original did, never more or less of the street. The
  reference size and the start-fullscreen flag are `Display` in game-tuning.json. Fullscreen:
  both objects' Create runs `window_set_fullscreen(true)` (the config's own `option_fullscreen` is
  False), so the title calls `GameDisplay.EnterFullscreen` and the player is set to start as a
  borderless fullscreen window at the display's resolution; F1 — on the title, in obj_game and in
  obj_debug alike, all `window_set_fullscreen(!window_get_fullscreen())` — is
  `GameDisplay.ToggleFullscreen`, between that and a window of the rooms' 1920x1080 port size
  (`option_sizeable` False, so not resizable), shrunk keeping 16:9 only on a display too small to
  hold it. Neither does anything in the Editor, whose Game view letterboxes the same way. Not
  ported: `option_interpolate` (GM's linear filter when stretching its application surface; the
  rebuild renders straight at the screen's resolution, so nothing is stretched), and **obj_startup's
  own screen** — RECONSTRUCTION §10 listed `startup` as unused, but it is first in the room order,
  so the original booted into it: bg_black and a red-outlined note that a plugged-in but switched-off
  wireless Xbox 360 controller slows "the development software" (GameMaker's runner), dismissed by
  any key or the left mouse. That note describes a GameMaker defect the Unity build cannot have, so
  showing it would be false; the rebuild boots straight to the title, which repeats obj_startup's
  display setup anyway.

- **Original content verified dead, decided not ported (U.INC-T33).** RECONSTRUCTION §10's full
  inventory audit (U.INC-T17) found these genuinely unreachable in the original — never placed in a
  room, never `instance_create`d, never called — so nothing of them is ported, and each reason below
  is checkable against the 09-05-13 source directly:
  - `obj_jukebox`, `spr_jukebox`, `spr_musicnote`: `obj_jukebox` sits only in the unused `gamescreen`
    room (not street00), and even there its one Step event just calls `play_random_music`, which is
    entirely commented out (RECONSTRUCTION §8/U.INC-T11), so it has no effect where it sits; its own
    sprite `spr_jukebox` is never drawn. `spr_musicnote` belongs to a different, live object —
    `obj_music`, the real per-room `current_track` setter (U.INC-T11) — but every room places
    `obj_music` at `x = -96`, off the 1920px-wide view, so that sprite is never seen either. Neither
    sprite is in game-content.json's `Art` catalog.
  - `obj_fade_stealth`: a second fade object (`spr_linewall`, depth -10000) with a
    `fadeout`/`fadehalf`/`fadein`/`off` state machine keyed on mouse height. Never created anywhere;
    `obj_fade` (`spr_black`) is the one the original actually creates and the one the rebuild ported
    (`spr_linewall` is catalogued `Live: false` in the Art catalog, U.INC-T29).
  - `obj_flash_text`: a floating, colour-cycling (white/red/yellow/blue) text object that fades and
    rises. No sprite, never instance-created by any script or room — an earlier, unused take on
    floating text; `show_float_text`/`obj_float_text` (U.INC-T31) is the one actually wired, and
    that's the one the rebuild ported as `FloatText`.
  - `obj_text`/`show_text`: `show_text(x, y, text)` instance-creates `obj_text`, which draws `text`
    in fnt_large with an outline 200px above its spawn point. `show_text` itself is never called
    anywhere in the project, so the object is unreachable.
  - `obj_ipaddressbox`: a full typed-input widget — an IP-address-style text box with an i-beam
    caret, tab-to-next-field, backspace and `sfx_type` per keystroke. Never placed in a room, never
    instance-created; reads like a discarded earlier take on the console UI, superseded by the typed
    command console actually wired (RECONSTRUCTION §6, U.INC-T10).
  - `obj_wall`: a plain collision rectangle (`spr_street00_01`, no events). Never placed in any room.
  - `object5`/`object8`: GameMaker's own default-named placeholders, never renamed from the editor's
    auto-generated "object5"/"object8". `object8` has no sprite and no events at all. `object5` has
    one Step event pinning itself to `obj_walker.x, obj_walker.y + 200` with `spr_arm` — an abandoned
    arm-position experiment, superseded by the real arm sprites and state machine the weapon system
    uses (RECONSTRUCTION §1, U.INC-T9). Neither is placed in any room.
  - `obj_typewriter_characterselect`: a second, unused typewriter object that types a `copystring`
    onto the HUD's bottom-left. Never placed or created; `obj_typewriter`/`type_text.gml` is the one
    actually used for the look/description teletype (RECONSTRUCTION §4/§6) and the one the rebuild
    ported as `DescriptionTypewriter`.
  - `spr_arm_left_support`: not referenced by any object's sprite or any `draw_sprite` call — a
    discarded arm pose; `spr_arm_left_aim1`/`spr_arm_left_aim2` are the ones actually used
    (RECONSTRUCTION §1).
  - `sprite_desaturate`: a stock GMLscripts.com grayscale-conversion utility pasted into the project
    but never called by any object or script.
  - Rooms `gamescreen` and `room2`: unused copies of the real rooms (RECONSTRUCTION §7) — no
    `next_room`/exit code ever names them, and neither is the project's start room. (`startup` is
    **not** in this group — see the U.INC-T32 entry above: it is the original's actual first room, a
    GameMaker-development-only controller warning the Unity build can't reproduce, so the rebuild
    boots straight to the title instead.)
  - The `_new` art and audio variants: live scripts and rooms reference only the legacy sprite/sound
    names; of the many `_new`-suffixed resources GameMaker's project tree implies, almost none
    survive as files on disk (only `music_gagool_new`, `sfx_flick_new`, `whispers_new` — RECONSTRUCTION
    §8), and even those are never the ones a live object plays, so the port uses the legacy files
    throughout (U.INC-T11, U.INC-T28).
  - `snd_step1`..`snd_step6` and `sound23`: each has its own `.sound.gmx` resource and ships audio
    files, but `play_footstep.gml` — the only footstep player — only ever `choose()`s among
    `sfx_step_concrete01`..`16`; nothing in any script, object or room references these seven sounds.
  - Music that never plays: of the project's ten `music_*` tracks, only `music_oppressivegloom`
    (street00, street01, and the titlescreen's global default) and `music_dubferal` (shop) are ever
    assigned to a room's `current_track` (U.INC-T11). `music_constancy2`, `music_constancy3`,
    `music_darknessiscoming`, `music_gagool`, `music_interloper`, `music_mta`, `music_tearoots` and
    `music_theme` are never selected by anything live — the only code that could have picked one at
    random, `play_random_music.gml`, is entirely commented out — and none of the eight are in
    game-content.json's `Sounds` catalog.
  - `obj_cloud1`..`obj_cloud4`, `obj_lamplight01`, `obj_prop_back`/`obj_prop_front`: already recorded
    as unused in the Rooms, props and audio decision above (U.INC-T11) — the clouds have no sprite
    and no room placement, the lamplight marker is never placed (each lamppost draws its own light
    sprite instead), and the back/front props are unplaced duplicates of the lamppost/mailbox the
    rooms actually use.
  - `obj_stealth`: the "scanning" dashboard object referenced only by other objects' dead branches,
    never created by anything live; already recorded in the Hacking overlay decision above
    (U.INC-T7) and catalogued `Live: false` in the Art catalog (U.INC-T29).
  - `obj_background` (U.INC-T42): `spr_street00_full` at depth 20, its create event pinning it to
    0,0. No room places it and nothing creates it; the rooms draw their numbered background slices
    instead (RECONSTRUCTION §7).
  - `obj_street00_00`..`obj_street00_05` (U.INC-T42): one object per `spr_street00_00`..`_05` slice,
    depth 20, no events at all. None is placed in any room or created by anything; the street's
    slices are room backgrounds (`bg_street00_*`), not objects, and `scenes.gml` (the only other
    user of those sprites) is never called.
  - `obj_room_startup`, `obj_room_street00`, `obj_room_street01` (U.INC-T42): earlier per-room
    copies of `obj_room`'s create event, never placed or created; every room in play uses the one
    generic `obj_room` (RECONSTRUCTION §7), which the rebuild ports. The street00 copy's poem and
    rain match what `obj_room` does for street00, so nothing of theirs is lost.
  - `obj_typewriter_gui` (U.INC-T42): a GUI-layer variant of `obj_typewriter` (type speed 2, fades
    out once typed, `spr_desc_border` box, fnt_large). Never placed or created; `obj_typewriter` is
    the one live typewriter, ported as `DescriptionTypewriter`.
  - `user_input_step` (U.INC-T42): a stock keyboard_string helper (credited "Davve,
    stuffbydavid.com") pasted into the project but never called by any object or script; the
    console's typing is `obj_console`'s own keyboard handling (RECONSTRUCTION §6).
  - The Android, iOS, WinPhone, Windows8, Tizen, Mac, Linux and html5 export configs
    (`Configs/Default/{Android,ios,WinPhone,Windows8,Tizen,Mac,Linux,html5}`): GameMaker: Studio's
    own auto-generated per-platform scaffolding (splash images, `option_android_*`/etc. fields baked
    into the one `Default.config.gmx`), created for every new project regardless of intent. The
    project only ever shipped a Windows build (RECONSTRUCTION §9's `Install\Incognito.exe`), so these
    carry no evidence of multi-platform intent and no gameplay to port.

- **Architecture to the rebuild standard (U.INC-T19 to T23, T25, T27).** The code was rebuilt to
  AGENTS.md's rebuild standard after the mechanics were ported; none of it changes gameplay.
  - *Assemblies (T19).* `Rea.Incognito.Core` (`Assets/Rea.Incognito/Core`, `noEngineReferences`, its own
    `Vec2`/`CoreMath` in place of `Vector2`/`Mathf`) holds every rule; `Rea.Incognito.Runtime`
    (`Scripts`) references Core; `Rea.Incognito.Editor` and the EditMode/PlayMode test assemblies sit
    on top. References are one-way and every asmdef is explicit.
  - *Core tests outside Unity (T20).* `Tests/Core.Tests` compiles the Core sources into a plain
    .NET test project run by `dotnet test` (`run-core-tests.ps1`); EditMode keeps the
    MonoBehaviour-layer tests and PlayMode the traversal, HUD and text tests.
  - *Commands and events (T21).* `GameWorld` is the one authority: presentation calls
    `Submit(GameCommand)`, every command carries a `PlayerId`, `Step` advances it, and what happened
    comes back as `GameEvent`s for presentation to play. MonoBehaviours no longer read each other's
    state, and obj_walker is no longer a global: there is no current-player singleton.
  - *Data (T22, T25).* Tuning (`Suspicion`, `Locomotion`, `Camera`, `Weapon`, `Rain`, `Display`)
    is `Resources/Data/game-tuning.json` behind `SettingsRegistry`; content (`Rooms`, `Npcs`,
    `Console`, `Sounds`, `Art`, `Fonts`, `TitleScreen`) is `game-content.json` behind `GameContent`.
    Both are parsed by Unity.Shared.Core's engine-free JSON reader and validated on load
    (`SettingsValidationException`), so `dotnet test` covers the parsing; `FieldCoverageTests`
    proves no field is silently dropped. `GameSettings` (Runtime) loads them once.
  - *Input System (T23).* `com.unity.inputsystem` is the only input handler. `GameInput` reads the
    keyboard and mouse into an engine-free `InputSnapshot` each frame; nothing calls
    `UnityEngine.Input`. Bindings are the original's keys (A/D or arrows, Shift, Z, Q, W, Escape,
    F1, F12, the mouse buttons and wheel), built in code rather than an `.inputactions` asset.
  - *Shared packages (T25).* `com.mindattic.unity.shared` and `com.mindattic.media.capture` are local
    `file:` packages from the sibling Unity.Shared and MindAttic.Media.Capture clones.
  - *Project hygiene (T27).* Company MindAttic, product Incognito, application id
    `com.MindAttic.Incognito`, no Unity splash; the manifest carries only the modules the game and
    its shared packages use; `tools/{unity,build,test}.ps1` are the batch build and test entry
    points (the Editor must be closed).

- **Final parity verification (U.INC-T35).** `ParityTourPlayTests` (PlayMode) plays the built title,
  Bleecher Street, 2nd Avenue and shop through a virtual keyboard and mouse and proves every row of
  RECONSTRUCTION section 11's checklist; `tools/verify-parity.ps1` runs it muted with screenshots. It
  found one real gap, fixed in the same ticket: the NPC was never a console object in a built room
  (the sandbox's `list sheep` placeholders were stripped with the rest of the sandbox), so `list
  sheep` answered "none" instead of the NPC's `SHP#0097`; the NPC now carries its own `ConsoleTarget`.
  - `decided:down-arrow-walkamount`: obj_walker's Down arrow takes 0.1 off `walkAmount` per step
    (RECONSTRUCTION section 9; its Up arrow is commented out). It is a development tweak that only
    ever shrinks the bob until the game restarts, and nothing in the original treats it as a
    mechanic, so it is not ported; the bob height is `Locomotion.BobAmplitude` in `game-tuning.json`.

- **Online-ready (U.INC-T43, rebuild standard point 9).** The original is single-player and has no
  online play; the rebuild adds the shared Unity.Multiplayer kit beside local play, which is untouched:
  the title, rooms, keys and scenes are exactly as before and contain none of the kit.
  - *Where it lives.* `Rea.Incognito.Online` (`Assets/Rea.Incognito/Online`, compiled only with MindAttic
    Components present) and `Rea.Incognito.Editor.Online`; `Incognito > Build Online Scene`
    (`OnlineSceneBuilder`) writes `Scenes/Online.unity` (the kit's NetworkManager, `NetBootstrap` and
    drop-in `NetFrontEnd`, the online room's background slices, the original 1920x1080 camera,
    `IncognitoOnlineGame`), the `IncognitoNetPlayer` prefab (spr_player at its GameMaker origin) and
    `mp-host.json`. It goes last in Build Settings (`RoomSceneBuilder` now keeps scenes it didn't make
    after its rooms), so a launch still opens the title; `-online` (`play.ps1 -Online`), or the kit's
    `-mode`/`-bot` launch switches, open it instead (`OnlineLaunch`). Its menu's `Play solo` goes back to
    the title with the kit torn down.
  - *One authority, commands carrying a seat.* Each connection's `IncognitoNetPlayer` is a `NetEntity`
    carrying its roster seat; seat N plays as `PlayerId` N+1 (`OnlineSeats`, Core), so a listen server's
    own seat is local play's `P1`. Its owner reads the same `GameInput` snapshot local play reads and sends
    it as `[ToServer]` calls; the server turns each into the same Core command local play submits
    (`SetControls`, `ToggleGun`, `PressAim`, `ReleaseAim`, `PressFire`, `ReleaseFire`) with the sender's
    seat, so a client only ever moves its own player. The controls travel packed (`OnlineControls`, Core)
    and the server makes a client's numbers safe (NaN, infinities, a cursor outside the view).
    `IncognitoAuthority`, on the server only, holds the session's `GameWorld`, steps it once a frame as
    `GameSession` does, and writes each seat's position, bob, facing, walk mode and aim mode into
    `[Synced]` fields every peer draws. A released seat is unseated (`GameWorld.RemovePlayer`).
  - *Data.* `game-tuning.json`'s `Online` section (`OnlineTuning`): the room a session plays in
    (`Street00`), the seat count (4) and the spacing between seats' starts (600px inward from the room's
    own start, seat 0 starting where local play does).
  - *Scope.* The online scene carries the players' walking and the gun's state on one street. The NPC,
    props, doors, the console and the bullets' presentation stay local-only until a ticket moves them;
    their rules already run in the one `GameWorld` the authority holds, so moving them is presentation
    work, not new rules.
  - *Proof.* Core `OnlineTests`; EditMode `OnlineSceneTests` (the scene's wiring, the title first and the
    local rooms free of the kit, the launch switches, the kit's prefab validator); PlayMode
    `IncognitoNetPlayerTests` (`NetSessionTest`: seats, a client's input walking and arming only its own
    player on the server, unsafe numbers made safe) and `IncognitoBotTests` (`NetBotTest`: two
    `IncognitoBot`s join a dedicated server by code, walk and draw). `tools/test.ps1` runs only the game's
    own test assemblies, since Netcode for GameObjects is under `testables` for the test kit.

## Laws

See AGENTS.md for the non-negotiables (reconstruction is ground truth, data over hard-coding where
the original did, no unreviewable editor-only state, done = verified by play, one ticket per
commit).
