---
layer: release
status: living
---
# Unity.Rea.Incognito — Release charter

> The one finish line this repo is working toward. Every open ticket in this repo sits under one of the epics
> below, and every epic is part of the milestone. Work that does not fit is out of scope until this milestone is
> reached (owner, 2026-11-27, MAT-T319).

## 1. What this is
A Unity 6 rebuild of the GameMaker 8 *Incognito*: a 2D side-on point-and-click stealth adventure with a close camera and an eyeball cursor (BIBLE).

## 2. The milestone: Unity.Rea.Incognito faithful port {#milestone}
- **Milestone:** MS-86, "Unity.Rea.Incognito faithful playable port"
- **Published means:** Development and Release Windows players at `R:\MindAttic\Apps\Unity.Rea.Incognito\{Debug,Release}`
  (internal; not on a store).
- **The natural stopping point:** the port is already built; this milestone confirms it as a deployed, playable,
  bug-fixed Release player that plays as the original did, then stops. Owner rule: resurrect the original
  faithfully, fix bugs only, never change the gameplay.

## 3. Done when (acceptance checks)
1. build — `run.ps1 -Redeploy` builds both Windows players clean on main.
2. exists — `R:\MindAttic\Apps\Unity.Rea.Incognito\Release` holds a player that launches.
3. signoff — the owner plays the Release player end to end: title, walk the rooms, talk to NPCs, use doors and the console, through the last room.

## 4. Epics, in priority order
| Priority | Epic (ticket) | What it delivers | Features |
|---|---|---|---|
| 900 | U.INC-T63 Faithful port | Confirmed bugs fixed, a recorded Release play-through | U.INC-T64 Port bug fixes (990), U.INC-T65 Release play-through (980) |
| 800 | U.INC-T66 Windows release | Players deployed on R:, owner sign-off | U.INC-T67 Players on R: (890), U.INC-T68 Owner sign-off (880) |

## 5. Out of scope (not before this milestone)
- Any change or improvement to gameplay, controls feel or content: the owner rule is resurrect the original faithfully and fix bugs only.
- Original content verified dead (RECONSTRUCTION section 10): stays unported.
- New rooms, story or stealth systems beyond the original.
- Mobile, console or store builds.

## 6. Current verified state
- A play-through of the Windows build covers the room route; RoomTraversalPlayTests replays it; end-to-end story A15 done (U.INC-T13, 8e741d3).
- Bughunt U.INC-T51 landed (ed31f6d, abae13f).
- The shared run menu deploys to R: (eda7018, MAT-T311); nothing is under R:\MindAttic\Apps yet.

## 7. Decision record
- 2026-11-27 — charter drafted from README/BIBLE/RECONSTRUCTION/tickets; Legion vote: Approve, plurality quorum reached, 5/9 voters answered, all Approve; no dissent (the rest had no provider key).
- Triage: 2 open -> done 0, duplicate 0, wont-do 0, kept 2 (U.INC-T59 bughunt reworded to a bounded task); new tasks 2 (Release play-through, owner sign-off).