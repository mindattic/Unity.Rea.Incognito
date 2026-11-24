---
codex: 1
project: Unity.Rea.Incognito
layer: research
status: living
updated: 2026-10-25
---

# Incognito — reverse-engineering notes

Source: `R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\_PRE_2017\gmlIncognito\09-05-13_2.19am`
(GameMaker 8, `.gmx` project format, dated 2013-05-09 by the folder name). Read-only reference —
never edit it. Everything below is read directly out of its `objects/*.object.gmx` and
`scripts/*.gml` files, not inferred; file names are given so a future session can re-check the
original. The project is called "Incognito" in `Incognito.project.gmx`; the player object is
`obj_walker`, so the design doc below calls them "the player."

Pitch, from the owner: an experimental point-and-click adventure inspired by Shadowrun, Room 23's
(Nintendo DS) art style, and the idea that a tight, close-in camera makes for a more intimate game.

## 1. Player locomotion — `objects/obj_walker.object.gmx`

Not a literal `sin()` call — it's a **hand-rolled triangle wave**, which is what reads to the eye
as "sinewave movement":

```
walkUp = true; walkAmount = 5.0; walkSpeed = 0.3; walkY = 0;
// per step, while mode == "walking":
if (walkUp) { if (walkY < walkAmount) walkY += walkSpeed; else { walkUp = false; play_footstep(...); } }
else        { if (walkY > -walkAmount) walkY -= walkSpeed; else walkUp = true; }
x += dx;
y = round(initialY + walkY);
```

`walkY` ramps linearly between `-walkAmount` and `+walkAmount` at `walkSpeed` per step, flipping
direction (and firing a footstep sound) at each extreme — a bob, not a bounce. `initialY` is fixed
at `room_height - sprite_height/2` (the player's feet never leave the street).

State machine: `mode` is `"idle" | "walking" | "stopping"`. Left/right key-down (events 65/68)
sets `mode = "walking"`, flips `facing`/`image_xscale`, and accelerates `dx` toward
`walk_speed` (double speed while holding Shift, ramped by `+= 0.1`/`-= 0.1` per step rather than
snapping). Key-up (events 65/68 release) sets `mode = "stopping"`, which applies `dx *= 0.9`
friction until `abs(dx) < 1`, then `mode = "idle"`.

**Aiming** is a separate state (`aimmode`: `"off" | "equip" | "aiming"`). Re-read for U.INC-T9
(GM mouse event numbers: 53/54 = global left/right *pressed*, 56/57 = global left/right
*released*):

- **Draw/holster** — `scripts/toggle_gun.gml`, called from the `Z` key-press event (`9`/`90`) and
  from clicking the HUD `obj_icon_pistol` (`obj_mouse_gui`). `"off"` -> `"equip"` plays
  `sfx_reload` and puts arm2 in `spr_arm_right_drawing`; anything else -> `"off"` plays
  `sfx_holster` and returns both arms to `spr_arm` (so holstering works mid-aim too).
- **Aim** — right *pressed* (54), only from `"equip"`: `aimmode = "aiming"`, `mode = "idle"`,
  `dx = 0`, `sfx_whoosh`, arm1 -> `spr_arm_left_aim1`, both arm angles zeroed, `alarm[0] = 5`.
  Five steps later the alarm (event `2`/`0`) settles arm1 -> `spr_arm_left_aim2` and arm2 ->
  `spr_arm_right_aim`. Right *released* (57) drops back to `"equip"` (arm2 drawing, arm1 rest). The
  walk-key events are gated on `aimmode != "aiming"`, so you can't walk or turn while aiming —
  aiming is *hold* right button, not a toggle.
- **Fire** — left *pressed* (53) while aiming: arm2 -> `spr_arm_right_fire` and `shoot_bullet()`;
  left *released* (56) puts arm2 back to `spr_arm_right_aim`.

`shoot_bullet.gml`: the bullet spawns 500px along arm2's angle from its pivot (mirrored when facing
left) and then 88px up, travels at `speed = 60` px/step in the arm's direction (`image_angle - 180`
facing left), plays `sfx_fire_cannon` and puffs `ef_smoke` at the muzzle. It also nudges both arms'
`x` by 10px along the shot, but that's a no-op: the arms' own step event re-pins `x` to the player
before they are drawn. `obj_bullet` (`spr_bullet`, 12x12) just destroys itself once it leaves the
room. `obj_npc`'s collision with `obj_bullet` shoves the NPC 100px away from the side the bullet
came from and destroys the bullet — no health, death or suspicion change is wired to a hit.

**Unity port note:** `ef_smoke` is one of GameMaker's built-in particle effects (`effect_create_below`
draws it below every instance), not a script or sprite in the project, so there is nothing in
`scripts/` or `sprites/` to read its particle count, size or lifetime from. The rebuild's
`MuzzleSmokeState` approximates it with its own small values (U.INC-T41): a handful of black
particles that drift sideways and rise from the muzzle while fading out over about half a second.

**Arm swing** — `objects/obj_arm.object.gmx` — is the *other* oscillator in the game, also a
triangle wave, not trig:

```
if (swing_forward) { image_angle -= rot_speed; angle += rot_speed; if (angle > swing_range) { angle = 0; swing_forward = false; } }
else                { image_angle += rot_speed; angle += rot_speed; if (angle > swing_range) { angle = 0; swing_forward = true; } }
```
Only runs while `player.mode == "walking"` and not aiming; relaxes back toward 0 when idle (and
holds still while `"stopping"` — there's no branch for it). Defaults `swing_range = 3`,
`rot_speed = 0.06`; the walk-left key sets 6/0.24 while Shift is held and 3/0.06 otherwise (the
walk-right key never touches them). `arm1` starts `swing_forward = true` and `arm2` `false`
(`scripts/start_game.gml`), so the arms swing in opposition. Both sit at `player.x ± 60` (behind
the facing direction), `player.y + 220`; arm1 draws in front of the body (depth -1), arm2 behind
(depth 1). While aiming, `image_angle` is driven directly by mouse Y (`(mouse_y - room_height/2) /
30`, mirrored by facing — at most ±18°) — the arm points at the cursor.

**Unity port note:** reproduce both oscillators as an explicit ping-pong ramp (`Mathf.PingPong` or
a hand-rolled increment, matching the *linear* feel) rather than `Mathf.Sin` — a true sine reads
as subtly different (eases at the extremes) from what the original actually does.

## 2. Camera — `objects/obj_camera.object.gmx`

This is the "close, intimate" framing the pitch describes. The camera doesn't center the player —
it pins the view near the edge **in the direction the player is facing**, so there's always more
room ahead of the player than behind:

```
if (player.x >= view_wview/2 + player_width && player.x < room_width - view_wview/2 - player_width) {
    if (player.facing == left)  x = player.x - view_wview/2 + player_width;
    else                        x = player.x + view_wview/2 - player_width;
} else {
    x = player.x; // clamp near room edges
}
y = mouse_y; // vertical pan follows the mouse directly
```

(`player_width` is cached once in the Create event from `spr_player`, 512px wide.) Vertical framing
is tied straight to `mouse_y`, uncapped in this snippet — the rebuild should decide a sensible
vertical clamp/deadzone since the original room is a flat 1080px-tall strip with the player's feet
fixed.

`obj_camera` is only the *target*; the actual view follows it through GM8's view settings in
`rooms/street00.room.gmx` (view 0: `objName="obj_camera" wview="1920" hview="1080" hborder="910"
vborder="32" hspeed="20" vspeed="-1"`). So the view keeps the camera object within a 50px
deadzone either side of centre (960 - 910), moves at most 20px per step horizontally (a facing flip
is a ~0.75s swing across 896px, not a cut), and GM clamps it to the room. Because `hview` equals
the room height, the view can never move vertically: `y = mouse_y` had no visible effect in the
original. The rebuild's vertical pan is therefore a design decision (BIBLE.md, Decisions made).

View object is swapped to the cursor (`view_object[0] = cursor`) when a console/hacking overlay is
"inverted" on (see §5) — the camera handoff that makes the HUD scan feel like you're now looking
*through* the UI rather than the world.

## 3. Eye-tracking cursor — `objects/obj_eye.object.gmx`, inlined copies in `obj_walker` and
`objects/obj_npc.object.gmx`, and the literal OS cursor object `objects/obj_mouse.object.gmx`

Two separate things share the name "eye":

- **The character's eyeball** (drawn in-world, at the player/NPC's eye socket) — a
  `spr_eyewhite` + `spr_eye_ball` (pupil) + `spr_eyelid_left`/`spr_eyelid_right` stack. Socket
  position flips with facing (`offsetX` swaps between a left-facing and right-facing constant).
  Pupil position is **mouse-relative parallax**, not literally mouse-locked:
  ```
  pupiloffsetX = 7 + ((mouse_x - player.x) / 160);
  pupilX = round(x + pupiloffsetX);
  pupilY = round(y - 3 + (mouse_y / 160));
  ```
  Dividing by 160 is what keeps the pupil inside the socket instead of flying off with the real
  cursor — it's a small, damped offset, not 1:1 tracking. While aiming, `pupiloffsetX` is instead
  snapped to a fixed in/out-facing value (no tracking) because the character is staring down the
  sights. **Blinking** is a randomized idle timer: each step, `irandom_range(0, eyelid_delay) == 0`
  (eyelid_delay = 160) has a 1-in-160 chance to start a blink; once started, `eyelidDY` ramps the
  eyelid sprite down over the eye then back up (closing/opening), gated by an `eyelid_closing`
  flag and an `eyelid_direction` state machine — the same shape as the walk bob, reused for blinks.
  NPCs (`obj_npc`) run an identical copy of this logic for their own eyes, plus a suspicion-percent
  label drawn above their head (see §4).

- **The OS mouse cursor** (`obj_mouse`, sprite `spr_cursor_eye` by default — the *cursor itself is
  drawn as an eye/eyeball icon*, separate from the in-world character eyeballs above). It tracks
  real mouse position 1:1 (`x = mouse_x; y = mouse_y;`) and swaps its own sprite by `mode`:
  `"look"` -> `spr_cursor_eye`, `"talk"` -> `spr_cursor_talk`, `"pointer"` -> `spr_cursor_pointer`,
  or `spr_cursor_reticle` whenever `player.aimmode == "aiming"` (aim reticle overrides mode). A
  global **right press** (mouse event 54) toggles `mode` between `"look"` and `"talk"` only —
  `"pointer"` is set elsewhere (by an NPC left-click, see §5) and is not reachable by toggling.
  (Re-read for U.INC-T6: events 53/54 are global left/right *pressed*, not the wheel — GM's wheel
  events are 60/61 and nothing in the project uses them. obj_mouse's 53 is an empty,
  commented-out click-to-walk. The same right press also enters aim from `"equip"` in obj_walker,
  so with the gun out it aimed and silently flipped the mode underneath the reticle.)
  `last_mode` is kept so the console toggle (§5) can restore the mode after forcing `"pointer"`.

**Unity port note:** this is two distinct eye systems — a diegetic in-world eyeball glued to each
character's face (parallax pupil + blink) and a cursor-replacement icon shaped like an eye. Both
should exist; don't collapse them into one object.

## 4. NPCs and the suspicion/stealth meter — `objects/obj_npc.object.gmx`

NPCs reuse the player's walk-bob and eye-tracking verbatim, plus:

- **Patrol**: walk toward `waypoint_x` at a fixed `walk_speed`, flipping `facing`/`image_xscale`
  toward the target (`a_near_b(x, waypoint_x, 20)` — `scripts/a_near_b.gml` — gates "arrived").
  Details (re-read for U.INC-T8): `walk_speed = 5` is a global (`scripts/globals.gml`); the NPC
  starts `"idle"` facing right and a 2-step `alarm[0]` flips it to `"walking"`. There is exactly
  one waypoint, `waypoint_x = previous_waypoint_x = 12000` — past street00's 11520px end — and the
  street00 instance stands at x = 2400, so the original NPC simply walks right off the street.
  Arrival does nothing: `hspeed` is left as is, so an NPC that did reach its waypoint would
  overshoot and jitter back and forth around it. The `"stopping"` branch (`hspeed *= 0.9` until
  `< 1`) exists but nothing in `obj_npc` ever enters it.
- **Suspicion** (a visible `%` label drawn over the NPC's head via `draw_text_outline`) is a
  per-step accumulator, `suspicion += suspicion_rate`, where `suspicion_rate` is recomputed every
  step from three independent cues (comment headers are the original author's own, kept verbatim):
  ```
  // How Close?
  if   (distance(player, npc) <= view_wview/2) suspicion_rate = 0.2;
  elif (distance(player, npc) <= view_wview)   suspicion_rate = 0.1;
  else                                          suspicion_rate -= 0.1;

  // Are they facing each other?
  if (facing_each_other) {
      // is Player looking down? (i.e. console/menu open, mouse_y > room_height/2)
      if (looking_down) suspicion_rate -= 0.2;
      else              suspicion_rate += 0.1;
  } else {
      suspicion_rate -= 0.1;
  }
  ```
  So: being close and in view *and* facing each other while the player *isn't* "looking down"
  (hasn't opened the console/HUD) raises suspicion fastest; looking away, being far, or having a
  menu open all bleed it off. This is the Shadowrun-flavored social-stealth system — no cover
  mechanic, just proximity + mutual eye contact + whether you looked like you were doing something
  else. `suspicion` itself is never clamped or consumed in what's wired up here (no detected/alert
  state fires) — the rebuild should decide thresholds and consequences; the original only draws
  the running percentage.

  Three literal-source details the summary above flattens (re-read for U.INC-T8):
  `distance` is `point_distance(player.x, 0, x, 0)` — horizontal only. The far branch is
  `suspicion_rate -= 0.1` with no reset, so out of range the rate keeps falling every step (the
  facing adjustment also accumulates there) and suspicion drains faster the longer you stay away;
  the near/mid branches assign, so stepping back into range resets it. And `facing_each_other` is
  only `npc.facing` opposite `player.facing` — position is never checked, so standing back to back
  also counts. `suspicion` is unclamped and can go negative.
- **Interaction** depends on the cursor's `mode` at click time (collision/left-click event,
  `eventtype="6" enumb="4"`): `"talk"` sets `mode = "talking"` and walks the NPC to stand next to
  the player; `"look"` calls `type_text(mouse_x, mouse_y, description)` (the NPC's floating
  description string, e.g. *"A young woman with tell tale red eyes and a meandering gait. #Bless
  the young and stoned."* — `#` is this engine's inline newline); `"pointer"` opens the
  hacking/console overlay targeted at that NPC (see §5) and switches the active camera view to it.
  Talk details (re-read for U.INC-T6): the click sets `waypoint_x = player.x + sprite_width` when
  the NPC is left of the player (else `player.x - sprite_width`) — the *far* side, so the NPC walks
  through the player. The walking branch runs for `"talking"` too (bob, head for the waypoint), and
  like patrol it never stops on arrival; the stop-and-face-each-other block (NPC faces the player,
  player turned to face the NPC) sits inside the `"stopping"` branch under `if (mode ==
  "talking")`, so it can never run. While the player holds a walk key, obj_walker re-arms every
  talking NPC's `alarm[0]` (200 steps holding right, 100 holding left); when it fires the NPC goes
  back to `"walking"` toward `previous_waypoint_x`. `type_text` destroys any typewriter already
  printing the same text, then spawns `obj_typewriter` at the click: one character per 2 steps,
  then `alpha -= 0.01` per 2 steps until gone, over a 0.5-alpha `spr_desc_border` box drawn from
  (x - 40, y - 20).
  NPCs also carry a `sheep_id` (e.g. `"0097"`) used by the `list sheep` console command (§6) —
  "sheep" is the in-fiction slang for a targetable civilian.

## 5. Hacking / console overlay — `objects/obj_stealth.object.gmx` (despite the file name, this is
the HUD-fade/"scanning" overlay, not a stealth mechanic — naming drift in the original project),
`objects/obj_console.object.gmx`, `objects/obj_gui_console.object.gmx`, `objects/obj_program.object.gmx`-family

A full-room black overlay (`spr_linewall`, scaled to room size) that cross-fades (`image_alpha +=
/-= fadespeed` at 0.025/step) between `"off" | "fadein" | "fadeout" | "scanning"` modes. Clicking
an NPC in pointer mode (§4) sets `mode = "scanning"`, which on its first pass brings three
`obj_program` icons and an `obj_icon_pistol` "onscreen" and settles the overlay at 50% alpha — a
translucent cyberpunk dashboard laid over the paused scene, themed like you've turned the NPC into
a terminal. `inverted` (toggled by a key event in `obj_walker`, 81) is the "full console" variant:
it switches `view_object[0]` to the cursor itself (camera now follows the UI, not the world),
forces cursor `mode = "pointer"`, and fades `gui_console` in/out — this is the state where you're
no longer looking at the street, you're looking at the terminal.

Re-read for U.INC-T7 — what the source actually wires, which differs from the summary above:

- `obj_stealth` is never created (no room places it, nothing calls `instance_create` on it), and
  nothing anywhere sets any overlay's `mode = "scanning"`, so the 50%-alpha `spr_linewall`
  dashboard and the three-programs-plus-pistol rise never run. `obj_gui` and `obj_gui_menu` are
  near-copies with the same dead "scanning" branch.
- **The console toggle** is obj_walker's key event 81 (Q) flipping the global `inverted`. On:
  `cursor.mode = "pointer"`, `over_gui = true`, `gui_console.mode = "fadein"`,
  `view_object[0] = cursor`, `view_hborder[0] = 32`, `view_hspeed[0] = 10` — the view now
  edge-scrolls with the cursor. Off: `cursor.mode = cursor.last_mode` (only ever set to "look", in
  obj_mouse Create), `over_gui = false`, `gui_console.mode = "fadeout"`, `view_object[0] = camera`,
  `view_hspeed[0] = 20`. This is the only way into pointer mode.
- `gui_console` (`obj_gui_console`, created by `scripts/start_game.gml`) draws `spr_circuitwall1`
  (1920x1080, ~10% alpha circuit traces) in the GUI layer, fading `image_alpha` by 0.025/step up
  to 1 on "fadein" and down to 0, then "off", on "fadeout".
- **The pointer click** on an NPC (obj_npc left-pressed, `cursor.mode == "pointer"`):
  `program[0].state = "onscreen"`, `program[0].sprite_index = spr_program_active`,
  `console[0].state = "onscreen"`, `console[0].sprite_index = spr_console_selected`,
  `gui_console.selected_object = object_index` (never read), `view_object[0] = object_index`,
  `view_hborder[0] = view_wview/2` — the view centres on the NPC.
- `program[0..2]` (`obj_program`, spr_program 113x86) are created by start_game.gml at x = 40,
  180, 320, resting just below the screen (`initialY = room_height`, +40, +80). "onscreen" eases
  up `y -= 1 + abs(y) * 0.01` per step to `endY = room_height - sprite_height`; "offscreen"
  drops 10px/step back to `initialY`. Only program[0] is ever sent onscreen.
- `console[0..2]` (`obj_console`, spr_console 580x200) are the typed command prompt again, at
  `y = room_height - 300`, input line at `y + 170`, output at `y + 35`, and a right-aligned
  `"CPU: " + round(cpu_power/active_cpus) + "/" + cpu_power + "%"` (or `cpu_power/cpu_power`
  with no active CPUs; `cpu_power = 100`) at `(inputX + 450, y + 8)`. Its slide-in/out and
  keyboard handling match obj_command_prompt (§6). It only takes keys while
  `selected_id == program_id` (`selected_id` starts at 0, so console[0] does).
- `obj_mouse_gui` handles left clicks on the GUI layer: on a program icon it plays `sfx_flick`
  (the open/close-its-console logic is commented out); on a console it selects it; on the pistol
  icon it calls `toggle_gun()`. `active_cpus` is only changed in that commented-out code, so
  obj_walker's `active_cpus == 0` movement gate never closes.

## 6. Typed command console — `scripts/exec_command.gml`, `scripts/command_list.gml`,
`scripts/split_string.gml`/`string_split.gml`, `objects/obj_command_prompt.object.gmx`,
`objects/obj_typewriter.object.gmx`

A literal typed-command terminal, lower-cased and space-split (`split_string`), dispatched by a
switch statement:

- `help` — lists `list` and `reboot`/`overwrite` (the latter two are referenced in the help text
  but have no case in the switch yet — stubs in the original).
  `list` — delegates to `command_list.gml`, itself a nested switch: `list all` (stub, "TODO: list
  all"), `list sheep` (enumerates every `obj_npc` instance's `sheep_id` as `SHP#<id>`).
- `clear` — empties the output.
- anything else — `"Command not recognized"`.

Output strings use `#` as an explicit line break (consistent with NPC `description` strings) and
`##` for a blank line. `obj_command_prompt` itself draws the output in one go (its output fade,
`alarm[1]`, is commented out, so output stays until replaced or `clear`ed); `sfx_type` plays per
keystroke (not while Shift is held). The character-by-character teletype is
`obj_typewriter`/`scripts/type_text.gml`, used for the `look` descriptions (§4), not the console.
The prompt is a 580x200 panel that eases in from the left (`x += 1 + ceil(|x| * 0.1)` per step),
slides out at 20px/step, and blinks a `_` i-beam every 30 steps.

## 7. Rooms and world — `rooms/*.room.gmx`, `objects/obj_room*.object.gmx`, `objects/obj_cloud*`,
`objects/obj_rain_emitter.object.gmx`, `objects/obj_prop_*`

Seven rooms: `titlescreen`, `startup`, `street00`, `street01`, `shop`, `gamescreen`, `room2`.
`street00` is a single wide side-scrolling strip — 11520×1080 px at 60 steps/sec — confirming the
whole game plays out on one long street rather than discrete screens. Parallax/atmosphere layer:
four `obj_cloud1..4` (a `obj_cloud_parent` drives shared drift behavior), `obj_rain_emitter` +
`obj_raindrop` for weather, `obj_lamplight01` for pooled light under streetlamps, and
`obj_prop_lamppost`/`obj_prop_mailbox`/`obj_prop_back`/`obj_prop_front` for foreground/background
dressing (front props render in front of the player, back props behind — a cheap depth trick worth
keeping as sorting layers in Unity). Numbered backgrounds (`bg_street00_00`..`_05`,
`bg_street01_00`..`_07`, `bg_shop_00`..`_02`) are 1920×1080 slices laid edge to edge as room
tiles (depth 1000000).

Corrections from re-reading the rooms for U.INC-T11:

- **No parallax on the streets.** The slices are tiles at one depth that scroll 1:1 with the
  view; nothing moves at a different rate. The only scrolling layer is the title screen's
  `bg_city_black` (tiled, `hspeed = -2`) under the opaque `bg_titlescreen`.
- **The clouds were never used.** `obj_cloud1..4` have no sprite and no instance in any room
  (`obj_cloud_parent` only drifts at `hspeed -1`). `obj_lamplight01` is not placed either: each
  `obj_prop_lamppost` draws `spr_lamplight01` itself, 100px above its top, blanking it on an
  `irandom_range(0,80) == 0` roll each step. `obj_prop_back`/`_front` are unplaced duplicates of
  the lamppost/mailbox; the rooms use `obj_prop_lamppost` (depth 10) and `obj_prop_mailbox`
  (depth -10), each typing its description on a left click.
- **Room names** (U.INC-T42): `obj_room`'s create event sets `name = get_room_name(room)`, and
  `get_room_name.gml` maps `street00` to "Bleecher Street", `street01` to "2nd Avenue" and `shop`
  to "Simon's Pawn and Loan" (anything else to ""); obj_room's draw-GUI event draws it top-left.
  Ported as each room's `Name` in game-content.json, drawn by `Room`.
- **The per-room controllers that were never placed** (U.INC-T42): `obj_room_startup`,
  `obj_room_street00` and `obj_room_street01` are earlier, per-room copies of `obj_room`'s create
  event (entry placement by `enter_from`, the fade-out; street00's also types the poem and
  creates the rain and `obj_jukebox`, street01's the rain), drawn with `spr_room_startup`. No room
  places any of them and nothing instance-creates them; every room in play places the one generic
  `obj_room` instead, which is what the rebuild ports.
- **Rain**: `obj_rain_emitter` makes 6 `obj_raindrop`s per step at a random x across the view
  and a random y up to one room above; each falls 40–50px and drifts 3–6px left per step (spr_rain,
  1×4, alpha 0.75, depth -1) and is destroyed below the room.
- **Rooms in play**: `titlescreen` (any key / left mouse → `start_game()`, Esc quits, F1 toggles
  fullscreen) → `street00` "Bleecher Street" (11520 wide; 4 lampposts, 2 mailboxes, 1 NPC, rain,
  the intro poem typed at `room_width - 650, 100`, a floating `obj_exit` door to `shop` at
  9504,480 entered with W within 300px, side exits to `street01` at both ends) ↔ `street01` "2nd
  Avenue" (15360 wide, blockout art, rain, side exits back to `street00`) and `shop` "Simon's Pawn
  and Loan" (5760 wide, blockout art, an `obj_barrier` at x 800, a right-edge exit back to
  street00 at x 9500). `obj_exit_side` sets `next_room`/`enter_from`, puts the player on
  `autowalk` and fades; `obj_room` places the player at `room_width - player_width` ("right"),
  `player_width` ("left") or `enter_x` ("x") and draws the room's name at 20,20. `gamescreen` and
  `room2` are unused copies. (Correction, U.INC-T32: `startup` is not; it is first in the room
  order and runs obj_startup before the title — §10.)

## 8. Audio inventory — `sound/*.sound.gmx`

Footsteps: 16 `sfx_step_concrete##` variants cycled by `scripts/play_footstep.gml` (called once per
walk-bob cycle, §1) for non-repetitive footfall. `sfx_whoosh` on facing-flip, `sfx_holster`/
`sfx_reload` for the weapon, `sfx_door_open`, `sfx_fire_cannon`, `sfx_type` per teletyped character
(§6), and `whispers`/`whispers_new` (ambient). Eight music tracks (`music_constancy2/3`,
`music_darknessiscoming`, `music_dubferal`, `music_gagool`, `music_interloper`, `music_mta`,
`music_oppressivegloom`, `music_tearoots`, `music_theme`) selected by `scripts/play_random_music.gml`.
Most assets have a legacy and a `_new` variant (re-recorded/normalized pass); prefer the `_new`
ones when porting — but almost none of the `_new` files survive in `sound/audio` (only
`music_gagool_new`, `sfx_flick_new`, `whispers_new`), so the port uses the legacy files.

Correction (U.INC-T11): **the original played no music.** `obj_music`'s `audio_play_music` call
and all of `play_random_music.gml` are commented out; only the per-room choice survives as data
(`current_track`: `music_oppressivegloom` for street00/street01 and, by the global default, the
title; `music_dubferal` for the shop). `sfx_type` plays on a command-prompt keystroke (not with
Shift, and not if already playing), not per teletyped character; `obj_typewriter` is silent.

## 9. Port verification — U.INC-T13

The other copy of the project, `R:\Projects\_PRE_2022\_PRE_2021\_PRE_2020\GameMaker\gmlIncognito`
(`Incognito.gmx`, saved 2018-10-28, with an `Install\Incognito.exe` build), matches the
09-05-13 snapshot above file for file except `obj_music` (its `audio_is_playing` guard is now
commented out too) and `play_random_music.gml` (the random pick is uncommented, but nothing
plays it). Neither copy plays music, so §8 holds for both.

Missed by the sections above: `obj_debug` (created by `start_game.gml`, persistent, so live in
every room) quits on Escape, toggles fullscreen on F1, toggles a debug readout (current room,
`player.x`, mouse and view coordinates) on F12. `obj_walker`'s Down arrow takes 0.1 off
`walkAmount` (the bob height); its Up arrow is commented out. `obj_titlescreen` starts the
window fullscreen.

Play-through of the Windows build (batch-built from `EditorBuildSettings`): title → any key or
click → Bleecher Street entered from the right; both side exits to 2nd Avenue and back; the
door to Simon's Pawn and Loan (W within 300px); the shop barrier at x 800; the shop's
right-edge exit back out at x 9500. Every arrival matched obj_room's placement. The console
view (Q), gun draw and aim (Z, hold right) with the reticle, rain, lamp light, mailboxes, the
intro poem and the suspicion % all showed up. The PlayMode test
`RoomTraversalPlayTests` plays the same route on the built scenes. Gaps went to tickets:
the character art is still placeholder (U.INC-T14), Escape/F1 only worked on the title
(U.INC-T15, since fixed by `DebugHotkeys`), and the dev sandbox scene and `Unity.Incognito` window title ship in the build
(U.INC-T16).

## 10. Parity and standards audit — U.INC-T17 (2026-10-23)

Full inventory of `09-05-13_2.19am` (60 objects, 22 scripts, 7 rooms, 88 sprites, 21 backgrounds,
70 sounds, 3 fonts) compared with the rebuild on main. Verdict: every behaviour sections 1-8 record
is built (locomotion, camera, eyes, cursor modes, NPC suspicion, weapon, typed console, hacking
overlay, four rooms with rain, lamps, exits, fades and music), so the remaining gaps are small
assets, the HUD gun icon, fonts and display, plus the standards debt below. Build order is the
ticket order U.INC-T19 to T35 (T19 first).

### Behaviour and assets the rebuild lacks (stories US-A10 to A15)

| Original | Gap | Ticket |
|---|---|---|
| `sfx_flick`, `whispers` sounds | not imported (flick is played on HUD/console clicks; whispers is referenced only in a commented line). Done in T28: both imported and catalogued; flick plays on a program-icon click (obj_command_prompt's flick call is inside a `/* */` block, so obj_mouse_gui is its only live player), whispers stays unwired | T28 |
| `spr_icon_pistol`, `_selected`, `spr_exit_side`, `spr_barrier`, `spr_command_prompt`(+selected), `spr_console`, `spr_linewall` | sprites not in Art/Original. Done in T29: all imported at their GM origins and catalogued in game-content.json's `Art`; the sweep also found `spr_brain_select` (obj_npc draws it over the NPC while `inverted`), `spr_cursor_arrow` (obj_mouse_gui's GUI cursor while `over_gui`), `spr_stealthwall` (obj_gui_menu, live: start_game creates it and it fades in while the mouse is over the bottom `sprite_get_height(spr_program)` px, not aiming and the console off; only its "scanning" branch is dead), `spr_black` (obj_fade) and `bg_black` (street00's base background). command_prompt and linewall belong to never-created objects (catalogued `Live: false`); spr_musicnote/spr_rainemitter are markers drawn at x -96, outside the room | T29 |
| brain-select overlay, GUI arrow cursor, obj_gui_menu dashboard hover fade | imported (T29) but not drawn. Done in T39: while `inverted`, obj_npc draws itself again under `bm_inv_dest_color, bm_inv_src_color` then `spr_brain_select` at its origin (`NpcController`, the `Incognito/InvertBlend` shader); the global `over_gui` (Q on sets it, Q off clears it, and obj_mouse_gui's left press sets it on a console, leaves it on a program or pistol icon and clears it on anything else) draws `spr_cursor_arrow` at the pointer in the GUI layer unless aiming, and hides obj_mouse's eye cursor (`if (!over_gui) draw_self()`; the rebuild keeps the reticle while aiming, a fixed defect); obj_gui_menu's `spr_stealthwall` fades by 0.025/step, in while the mouse is below 994 and out above it, its mode chosen only while `player.aimmode == "off"` (the gun holstered, not merely not aiming) and `gui_console.mode == "off"` (`DashboardMenu`) | T39 |
| `obj_icon_pistol` + `obj_mouse_gui` | HUD pistol icon and GUI-layer click handling (toggle_gun, sfx_flick, console select) absent. Done in T30: `HudPistolIcon` rises to 984 at x = 1784 (40px from the right), shows the selected sprite while the gun is out, a click sends ToggleGun; obj_mouse_gui's program/console/pistol chain is `MouseGui`, console clicks flick and select. The original never raised the icon (only dead "scanning" branches set it onscreen), so the rebuild raising it at play start is a fixed defect | T30 |
| `fnt_basic/large/system`, `draw_text_outline*`, `show_float_text`, `obj_float_text` | IMGUI default font and a single outline helper; no floating text. Done in T31: fnt_basic is Arial 12pt, fnt_large High Tower Text 16pt, fnt_system Lucida Console 8pt, their atlases rendered at 120 DPI (so 20, 26.7 and 13.3px on the 1080p view); catalogued in game-content.json's `Fonts`, drawn as Arial (the other two faces can't ship) at those sizes; draw_text_outline/_alpha/_color are `OutlinedText`'s styles; `#`/`##`/`\#` are `GmText`, content authored in that form; obj_float_text is `FloatText` (no live object calls show_float_text, so nothing shows one) | T31 |
| `obj_startup` | screen-ratio setup, fullscreen start, letterboxing. Done in T32: `ScreenRatio` (Core) fits the 1920x1080 view keeping its aspect (`option_scale = -1`), `GameDisplay` letterboxes every camera and the HUD with it and maps every screen reading through it, the title enters fullscreen and F1 toggles fullscreen/window as obj_titlescreen, obj_game and obj_debug do. Correction: the `startup` room is not unused — it is first in `Incognito.project.gmx`'s room order, so obj_startup's Create (screen ratio, `window_set_fullscreen(true)`) ran first and its screen showed, on bg_black, a red-outlined note (fnt_basic, centred at `room_height - 60`) that a switched-off wireless Xbox 360 controller slows "the development software", until any key or the left mouse went to `titlescreen`. That note is about the GameMaker runner, so the rebuild doesn't show it (BIBLE) | T32 |

### Original content verified dead (decided not ported, T33 records it in the BIBLE)

`obj_jukebox` (placed only in the unused `gamescreen` room, and even there its one Step event
just calls the fully commented-out `play_random_music`, §8), `obj_fade_stealth`,
`obj_flash_text`, `obj_text`/`show_text`, `obj_ipaddressbox`, `obj_wall`, `object5`, `object8`,
`obj_typewriter_characterselect`, `obj_cloud1..4`, `obj_lamplight01`, `obj_prop_back/_front`,
`obj_stealth`, `spr_arm_left_support`, `sprite_desaturate`, rooms `gamescreen`, `room2` (not
`startup`: it is the first room the original boots into, see the `obj_startup` row above),
the `_new` art/audio variants, `snd_step1..6`, `sound23`, music that never plays, and the
Android, iOS, WinPhone, Windows8, Tizen, Mac, Linux and html5 export configs.

### Unity standards audit

| Standard | State on main | Ticket |
|---|---|---|
| Assembly definitions, engine-free Core | one `Rea.Incognito.Runtime` asmdef; the `*State` classes import `UnityEngine`; `noEngineReferences` false | T19 |
| Core tested outside Unity | Done: `Tests/Core.Tests` run by `run-core-tests.ps1` (`dotnet test`, C# 9); EditMode keeps the MonoBehaviour layer, PlayMode traversal | T20 |
| Commands to events, player-id seams | MonoBehaviours read each other directly | T21 |
| Content and tuning as validated data | tuning is serialized fields on MonoBehaviours | T22 |
| Input System | legacy `UnityEngine.Input` (32 sites), `activeInputHandler: 0` | T23 |
| URP behind an agnostic layer | no render pipeline asset | T24 |
| Unity.Shared profiles/settings | package not referenced | T25 |
| Built in code, modern UI | 7 IMGUI `OnGUI` classes; scenes committed by hand-run builder | T26 |
| Project and package hygiene | `DefaultCompany`, splash on, every built-in module, no build script | T27 |
| README, BIBLE, story status | README says implementation not started; US-A1/A2/A3/A5/A6/A9 shown planned | T34 |

T35 is the final scripted verification; its checklist is section 11.

## 11. Parity checklist — U.INC-T35 (2026-10-25)

The final verification. `Assets/Rea.Incognito/Tests/PlayMode/ParityTourPlayTests.cs` plays the built
title, Bleecher Street, 2nd Avenue and shop through a virtual keyboard and mouse (the Input System
reads them exactly as it reads hardware) and proves each `tour:` row below with a `Row("id")` call;
`test:` rows are proven by the named existing test over the same built scenes, and `decided:` rows
are original behaviour deliberately not ported, recorded in BIBLE.md. `tools/verify-parity.ps1` runs
the tour muted (`-muted`, `MINDATTIC_CLI_MUTED=1`) and writes `Logs/parity-tour/NN-tag.png`
screenshots plus `Logs/parity-tour.txt`, the rows the run proved. `ParityChecklistTests`
(Core.Tests) fails on any unticked row, any `tour:` id the tour no longer proves, any `test:` class
that does not exist and any `decided:` id BIBLE.md does not record, so the list cannot rot.

The tour found one real gap, fixed here: the built rooms never listed the NPC as a console object,
so `list sheep` said "none" (the console row below now proves it lists the NPC's `SHP#0097`).

Screenshots: MindAttic.Media.Capture's `TakeScreenshot` is called at each moment, but its grab waits for the
end of a rendered frame, which a `-batchmode` Editor never reaches, so the batch run's record is the
tour's own PNGs (`tools/verify-parity.ps1 -Capture` turns Capture on for a run that has a real frame).

| Done | Row | Section | Mechanic proved |
|---|---|---|---|
| [x] | `tour:title.default-music` | 3 title | The title plays the global default track (music_oppressivegloom). |
| [x] | `tour:title.any-key-starts-the-game` | 7 / 1 | Any key on the title runs start_game(): street00, entered from the right, facing left. |
| [x] | `tour:room.arrival-and-name` | 7 | obj_room places the player by enter_from and draws the room's name. |
| [x] | `tour:room.opening-poem` | 7 | street00's intro poem is typed at the room's right end. |
| [x] | `tour:room.rain` | 7 | obj_rain_emitter's rain falls. |
| [x] | `tour:room.props-and-lamps` | 7 | 4 lampposts (each with its own flickering lamplight) and 2 mailboxes. |
| [x] | `tour:prop.click-types-description` | 7 | A left click on a prop types its description at the cursor. |
| [x] | `tour:audio.room-music` | 8 | The room's own track plays (street00: oppressivegloom). |
| [x] | `tour:audio.shop-music` | 8 | The shop plays music_dubferal. |
| [x] | `tour:audio.flick` | 5 / 8 | A GUI console click plays sfx_flick (footstep, reload, whoosh, fire and holster sounds are asserted in the walk and weapon rows; door open is the DoorUsed event in world.door-enters-the-shop). |
| [x] | `tour:walk.bob-and-footsteps` | 1 | Walking is a triangle-wave bob within walkAmount with a footstep at each peak. |
| [x] | `tour:walk.speed` | 1 | Walking speed is walk_speed. |
| [x] | `tour:walk.sprint` | 1 | Shift doubles the speed, ramped. |
| [x] | `tour:walk.friction-stop` | 1 | Key-up goes walking to stopping (friction) to idle. |
| [x] | `tour:walk.turn-flips-facing` | 1 / 2 | Walking the other way flips the facing and the camera's lead. |
| [x] | `tour:arms.swing-in-opposition` | 1 | The two arms swing in opposition while walking. |
| [x] | `tour:camera.leads-facing` | 2 | The camera pins the view ahead of the player, in their facing direction. |
| [x] | `tour:camera.clamps-at-room-edges` | 2 | Near the room's edges the camera stops at the room. |
| [x] | `tour:camera.vertical-pan-follows-mouse` | 2 | The vertical pan follows the mouse, bounded (a rebuild decision, BIBLE). |
| [x] | `tour:eye.socket-follows-facing` | 3 | The eye socket flips with the facing. |
| [x] | `tour:eye.pupil-parallax` | 3 | The pupil moves by mouse parallax (divisor 160), not 1:1. |
| [x] | `tour:eye.blink` | 3 | The eyelid blinks at random. |
| [x] | `tour:cursor.look-talk-cycle` | 3 | The eye cursor flips look/talk by right click (gun holstered) and wheel, with each mode's sprite. |
| [x] | `tour:cursor.reticle-while-aiming` | 3 | The reticle replaces the cursor while aiming. |
| [x] | `tour:weapon.draw-with-Z` | 1 | Z draws the gun: sfx_reload, drawing arm pose, HUD icon selected. |
| [x] | `tour:weapon.aim-locks-movement` | 1 | While aiming the player can neither walk nor turn; sfx_whoosh sounded. |
| [x] | `tour:weapon.fire-bullet-and-smoke` | 1 | Left click while aiming fires one bullet from the muzzle with a puff of smoke and sfx_fire_cannon. |
| [x] | `tour:weapon.bullet-despawns` | 1 | The bullet is destroyed once it leaves the room. |
| [x] | `tour:weapon.arm-follows-mouse-height` | 1 | The aim angle follows the mouse's height. |
| [x] | `tour:weapon.aim-is-a-hold` | 1 | Releasing right drops back to the drawn gun. |
| [x] | `tour:hud.pistol-icon-toggles-gun` | 5 | The HUD pistol icon draws and holsters the gun like Z (sfx_holster, selected sprite). |
| [x] | `tour:weapon.shot-shoves-the-npc` | 1 / 4 | A bullet meeting the NPC shoves it 100px away from the shot. |
| [x] | `tour:npc.patrols` | 4 | The NPC walks toward its waypoint. |
| [x] | `tour:npc.suspicion-rises-near-and-facing` | 4 | Close, face to face and eyes up raises suspicion. |
| [x] | `tour:npc.suspicion-label` | 4 | The % over the NPC's head shows the suspicion. |
| [x] | `tour:npc.looking-down-stops-suspicion` | 4 | Looking down cancels the face-to-face growth. |
| [x] | `tour:npc.far-bleeds-suspicion` | 4 | Far from the NPC, suspicion drains. |
| [x] | `tour:npc.look-types-description` | 4 | Look mode: a click types the NPC's description. |
| [x] | `tour:npc.talk-walks-to-the-player` | 4 | Talk mode: a click sets the NPC talking toward the player's far side. |
| [x] | `tour:console.q-toggles-inverted` | 5 | Q toggles the console view: pointer mode, circuit backdrop fades in. |
| [x] | `tour:console.brain-select-over-the-npc` | 5 / 10 (T39) | In the console view the NPC draws its inverted-blend copy and spr_brain_select over itself, and stops when the view goes down. |
| [x] | `tour:cursor.gui-arrow-while-over-gui` | 3 / 10 (T39) | over_gui (Q, a console press) draws obj_mouse_gui's arrow instead of the eye cursor; a press on the world clears it. |
| [x] | `tour:hud.dashboard-fades-over-the-bottom-strip` | 10 (T39) | With the gun holstered and the console off, the mouse over the bottom 86px fades spr_stealthwall in, and away from it fades it out. |
| [x] | `tour:console.pointer-click-opens-the-hack` | 5 | A pointer click on the NPC makes it the hack target, raises the program icon and slides the console in. |
| [x] | `tour:console.help-list-clear-unknown` | 6 | help, list sheep (the NPC's SHP id), clear and an unknown command. |
| [x] | `tour:console.escape-closes-the-hack` | 5 | Escape closes the console and ends the hack. |
| [x] | `tour:console.q-restores-the-cursor-mode` | 5 | Q again restores the cursor mode from before. |
| [x] | `tour:world.street00-right-exit-to-street01` | 7 | Bleecher Street's right edge leads to 2nd Avenue, entered from the left. |
| [x] | `tour:world.street01-left-exit-to-street00` | 7 | 2nd Avenue's left edge leads back to Bleecher Street, entered from the right. |
| [x] | `tour:world.door-enters-the-shop` | 7 | W within 300px of the door enters Simon's Pawn and Loan (not beyond it); it is dry indoors. |
| [x] | `tour:world.shop-barrier-blocks` | 7 | The shop's obj_barrier at x 800 stops the player walking west. |
| [x] | `tour:world.shop-exit-to-street00` | 7 | The shop's right edge leads out to Bleecher Street at x 9500. |
| [x] | `tour:world.street00-left-exit-to-street01` | 7 | Bleecher Street's left edge leads to 2nd Avenue, entered from the right. |
| [x] | `tour:world.street01-right-exit-to-street00` | 7 | 2nd Avenue's right edge leads back, entered from the left. |
| [x] | `test:RoomTraversalPlayTests` | 9 | The T13 route over the built scenes (arrival x and facing for every exit). |
| [x] | `test:OriginalTextPlayTests` | 10 (T31) | Room, NPC, console and floating text use the original fonts and outlines. |
| [x] | `test:OriginalFontParityTests` | 10 (T31) | fnt_basic, fnt_large and fnt_system match the original's .font.gmx. |
| [x] | `test:OriginalArtParityTests` | 10 (T29) | Every image a live original object or room uses is in the rebuild at its origin. |
| [x] | `test:OriginalAudioParityTests` | 10 (T28) | Every sound a live original object plays is catalogued, wired and on disk (sfx_type, sfx_door_open; whispers unwired). |
| [x] | `test:ScreenRatioTests` | 10 (T32) | The 1920x1080 view letterboxes on other aspects (obj_startup's screen ratio). |
| [x] | `test:GameDisplayTests` | 10 (T32) | The fullscreen/window toggle and the HUD's mapping through the letterbox. |
| [x] | `test:DebugHotkeysTests` | 9 | Escape quits, F1 toggles fullscreen, F12 shows the debug readout. |
| [x] | `test:MuzzleSmokePlayTests` | 10 (T41) | The muzzle smoke sorts behind every actor sprite. |
| [x] | `test:HudPistolIconPlayTests` | 10 (T30) | The pistol icon rises to room_height - 96 and obj_mouse_gui's console click selects and flicks. |
| [x] | `test:SessionAudioTests` | muted launches | The CLI mute silences the AudioListener without touching settings.json. |
| [x] | `decided:down-arrow-walkamount` | 9 | obj_walker's Down arrow (a dev tweak of walkAmount) is deliberately not ported; BIBLE records why. |

**Verdict.** Every row above is ticked: the rebuild reproduces every mechanic sections 1-8 record
that the original actually wired, plus the audit's assets, fonts and display (section 10), so the
repository is a complete proof of concept of the original *Incognito*. What it leaves out is on
purpose: the content section 10 verified dead, the Down-arrow tweak, the GameMaker-only startup note,
and the open questions below (suspicion reaching 100% has no consequence in the original, so none is
invented; `reboot`, `overwrite` and `list all` are stubs).

## Open questions the source doesn't answer

- No wired-up consequence for suspicion reaching 100% (no alert/detected state in the objects
  read) — the rebuild needs to design what happens, since the original only displays the number.
- `help`'s `reboot`/`overwrite` commands and `list all` are stubs with no implementation to port —
  treat as design-your-own, not reconstruction. (Decided in U.INC-T10 — see BIBLE.md "Decisions
  made".)
- No save/menu/inventory objects were found — this was a short vertical-slice prototype, not a
  full game; the rebuild's scope (per the ticket breakdown) covers the slice that exists.
