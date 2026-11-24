using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Incognito.CameraRig;
using Incognito.Eye;
using Incognito.Hacking;
using Incognito.Npc;
using Incognito.Audio;
using Incognito.Player;
using Incognito.Terminal;
using Incognito.Typing;
using Incognito.Weapon;
using Incognito.World;
using MindAttic.Media.Capture.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Facing = Incognito.Player.PlayerLocomotionState.FacingDirection;
using Object = UnityEngine.Object;

namespace Incognito.Tests
{
    /// <summary>
    /// U.INC-T35, the final parity verification: a scripted run that plays the game the way a person
    /// does, through the real input path (a virtual keyboard and mouse feed the Input System, so
    /// <see cref="Incognito.Controls.GameInput"/> reads exactly what it reads from hardware), over the
    /// built room scenes in the order the original was played: the title, Bleecher Street, 2nd Avenue
    /// and the shop. Every mechanic RECONSTRUCTION.md records is exercised and each proof is a
    /// <c>Row("id")</c>; the parity checklist in docs/RECONSTRUCTION.md section 11 has one row per id,
    /// and <c>ParityChecklistTests</c> (Core.Tests) fails if a row is unticked or names an id this
    /// file no longer has. Screenshots are taken at the key moments through MindAttic.Media.Capture
    /// (<see cref="CaptureUnity.TakeScreenshot"/>, on when tools/verify-parity.ps1 enables it) and are
    /// also written as PNGs to Logs/parity-tour when the run has a graphics device. Walking, aiming,
    /// firing, the cursor, clicks on NPCs, props, the pistol icon and the exits, and the Q, Z, W and
    /// Escape keys all go through that input path; the console's typed commands call the prompt's
    /// Submit (what Enter calls) and its GUI click calls GuiClicked, since a UI Toolkit text field
    /// can't be typed into by queued key events in batch mode.
    /// </summary>
    public class ParityTourPlayTests
    {
        private const float Px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
        private const float PlayerWidth = 512f;
        private const float Timeout = 60f;
        private const float StreetWidth = 11520f;

        private static readonly List<string> Passed = new List<string>();
        private static int shotNumber;

        private TourInput input;
        private float previousTimeScale;
        private readonly List<GameEvent> events = new List<GameEvent>();
        private GameSession session;

        [OneTimeSetUp]
        public void ResetReport()
        {
            Passed.Clear();
            shotNumber = 0;
        }

        [OneTimeTearDown]
        public void WriteReport()
        {
            string dir = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, "parity-tour.txt"), Passed.Distinct());
        }

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 4f;
            input = new TourInput();
            events.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            input.Dispose();
            Time.timeScale = previousTimeScale;
        }

        // ---------------------------------------------------------------- Title and Bleecher Street

        [UnityTest]
        public IEnumerator Tour_1_the_title_starts_the_game_and_Bleecher_Street_is_the_world_the_original_drew()
        {
            SceneManager.LoadScene("Titlescreen");
            yield return null;
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<TitleScreen>(), "the title has its obj_titlescreen");
            Assert.That(MusicPlayer.Current != null ? MusicPlayer.Current.name : "", Does.Contain("oppressivegloom"),
                "the title plays the global default track");
            Row("title.default-music");
            yield return Shot("title");

            // Any key starts the game: start_game() -> street00, entering from the right, facing left.
            yield return input.Tap(Key.Space);
            Room room = null;
            yield return Arrive("Street00", r => room = r);
            AttachSession();
            AssertArrival(room, "Bleecher Street", StreetWidth - PlayerWidth, Facing.Left);
            Row("title.any-key-starts-the-game");
            Row("room.arrival-and-name");

            Assert.That(MusicPlayer.Current != null ? MusicPlayer.Current.name : "", Does.Contain("oppressivegloom"));
            Row("audio.room-music");

            // The intro poem is typed at the room's right end.
            yield return Until(() => Object.FindObjectsByType<DescriptionTypewriter>(FindObjectsSortMode.None)
                .Any(t => t.State != null && t.State.Text.Contains("strange city")), 10f, "the opening poem is typed");
            Row("room.opening-poem");

            var rain = Object.FindAnyObjectByType<RainEmitter>();
            Assert.IsNotNull(rain, "street00 has its obj_rain_emitter");
            yield return Until(() => rain.State.Drops.Count > 0, 5f, "rain falls");
            Row("room.rain");

            var props = Object.FindObjectsByType<PropDescription>(FindObjectsSortMode.None);
            Assert.AreEqual(6, props.Length, "4 lampposts and 2 mailboxes");
            Assert.AreEqual(4, Object.FindObjectsByType<LampFlicker>(FindObjectsSortMode.None).Length, "each lamppost draws its own lamplight");
            Row("room.props-and-lamps");

            // A left click on a prop types its description where the cursor is.
            var prop = props.OrderBy(p => Mathf.Abs(p.transform.position.x / Px - room.Player.State.PositionX)).First();
            Send(room, new TeleportPlayer(room.Player.Id, prop.transform.position.x / Px - 200f));
            yield return OnScreen(prop.GetComponent<Collider2D>());
            yield return Shot("street00-poem-and-rain");
            yield return ClickCollider(prop.GetComponent<Collider2D>());
            yield return Until(() => Object.FindObjectsByType<DescriptionTypewriter>(FindObjectsSortMode.None)
                .Any(t => t.State != null && t.State.Text == prop.Description), 5f, "the prop's description is typed on a click");
            Row("prop.click-types-description");
        }

        // ---------------------------------------------------------------- Locomotion, camera, eyes

        [UnityTest]
        public IEnumerator Tour_2_the_player_walks_bobs_sprints_stops_and_the_camera_and_eyes_follow()
        {
            Room room = null;
            yield return EnterStreet(r => room = r);
            var player = room.Player;
            var camera = Object.FindAnyObjectByType<CloseFramingCamera>();
            var eyeball = Object.FindAnyObjectByType<PlayerEyeball>();
            var cursor = Object.FindAnyObjectByType<EyeCursor>();
            Assert.IsNotNull(camera);
            Assert.IsNotNull(eyeball);
            Assert.IsNotNull(cursor);

            Send(room, new TeleportPlayer(player.Id, 7000f));
            input.MoveMouseTo(ViewportPoint(0.5f, 0.5f));
            yield return null;

            // Hold left: a walk with the triangle-wave bob, a footstep at each peak.
            int footsteps = events.Count(e => e is PlayerFootstep);
            float startX = player.State.PositionX;
            float minBob = 0f, maxBob = 0f;
            bool arms = true;
            input.Hold(Key.A);
            float until = Time.time + 2.5f;
            while (Time.time < until)
            {
                minBob = Mathf.Min(minBob, player.State.BobOffset);
                maxBob = Mathf.Max(maxBob, player.State.BobOffset);
                arms &= player.Entity.Weapon.SupportArm.SwingingForward != player.Entity.Weapon.GunArm.SwingingForward;
                yield return null;
            }
            Assert.AreEqual(PlayerLocomotionState.WalkMode.Walking, player.State.CurrentMode);
            Assert.AreEqual(Facing.Left, player.State.Facing);
            Assert.Less(player.State.PositionX, startX - 100f, "walking left moved the player left");
            Assert.Greater(maxBob, 3f, "the bob rises above the resting height");
            Assert.Less(minBob, -3f, "and falls below it");
            Assert.LessOrEqual(Mathf.Max(maxBob, -minBob), player.State.BobAmplitude + 0.5f, "a bob, not a bounce");
            Assert.Greater(events.Count(e => e is PlayerFootstep), footsteps, "a footstep at the bob's peak");
            Row("walk.bob-and-footsteps");
            Assert.IsTrue(arms, "the arms swing in opposition");
            Row("arms.swing-in-opposition");
            Assert.AreEqual(player.State.WalkSpeed, Mathf.Abs(player.State.HorizontalSpeed), 0.6f, "walk speed");
            Row("walk.speed");

            // The camera pins the view ahead of the player, in the direction they face.
            yield return Until(() => camera.State.CenterX < player.State.PositionX - 150f, 10f, "the camera leads left");
            Row("camera.leads-facing");

            // Shift doubles the speed, ramped rather than snapped.
            input.Hold(Key.LeftShift);
            yield return WaitGame(2f);
            Assert.AreEqual(player.State.WalkSpeed * player.State.SprintMultiplier, Mathf.Abs(player.State.HorizontalSpeed), 0.8f, "sprint speed");
            Row("walk.sprint");
            yield return Shot("walking-sprint");

            // Let go: friction coasts the player to a stop instead of cutting out.
            Time.timeScale = 1f;
            input.Release(Key.A);
            input.Release(Key.LeftShift);
            yield return null;
            yield return null;
            Time.timeScale = 4f;
            Assert.AreEqual(PlayerLocomotionState.WalkMode.Stopping, player.State.CurrentMode, "key-up starts stopping");
            float coasting = Mathf.Abs(player.State.HorizontalSpeed);
            yield return Until(() => player.State.CurrentMode == PlayerLocomotionState.WalkMode.Idle, 5f, "the player comes to rest");
            Assert.Less(Mathf.Abs(player.State.HorizontalSpeed), coasting, "friction took the speed off");
            Row("walk.friction-stop");

            // Turning right flips the facing, the eye socket and which way the camera leads.
            input.Hold(Key.D);
            yield return WaitGame(2.5f);
            Assert.AreEqual(Facing.Right, player.State.Facing);
            Assert.AreEqual(eyeball.State.SocketOffsetRight, eyeball.State.SocketOffsetX, "the eye socket follows the facing");
            yield return Until(() => camera.State.CenterX > player.State.PositionX + 150f, 10f, "the camera leads right");
            input.Release(Key.D);
            Row("walk.turn-flips-facing");
            Row("eye.socket-follows-facing");
            yield return Until(() => player.State.CurrentMode == PlayerLocomotionState.WalkMode.Idle, 5f, "the player comes to rest");

            // Near the room's edges the camera stops at the room instead of leading.
            Send(room, new TeleportPlayer(player.Id, 300f));
            yield return Until(() => camera.State.CenterX >= 959f && Settled(camera), 30f, "the camera holds at the left edge");
            Assert.GreaterOrEqual(camera.State.CenterX, 959f);
            Send(room, new TeleportPlayer(player.Id, StreetWidth - 300f));
            yield return Until(() => camera.State.CenterX <= StreetWidth - 959f && camera.State.CenterX > 6000f && Settled(camera), 30f, "the camera holds at the right edge");
            Assert.LessOrEqual(camera.State.CenterX, StreetWidth - 959f + 1f);
            Row("camera.clamps-at-room-edges");

            // The vertical pan is the rebuild's own decision (BIBLE): bounded, and tied to the mouse.
            Send(room, new TeleportPlayer(player.Id, 6000f));
            input.MoveMouseTo(ViewportPoint(0.5f, 0.98f));
            yield return WaitGame(1.5f);
            float low = camera.State.VerticalOffset;
            input.MoveMouseTo(ViewportPoint(0.5f, 0.02f));
            yield return WaitGame(1.5f);
            float high = camera.State.VerticalOffset;
            Assert.AreNotEqual(low, high, "the camera's height follows the mouse");
            Assert.LessOrEqual(Mathf.Max(Mathf.Abs(low), Mathf.Abs(high)), camera.State.MaxVerticalPan + 0.01f);
            Row("camera.vertical-pan-follows-mouse");

            // The pupil is mouse parallax, not a 1:1 follow.
            input.MoveMouseTo(ViewportPoint(0.02f, 0.5f));
            yield return null;
            yield return null;
            int leftX = eyeball.State.PupilOffset.x;
            input.MoveMouseTo(ViewportPoint(0.98f, 0.5f));
            yield return null;
            yield return null;
            int rightX = eyeball.State.PupilOffset.x;
            Assert.Greater(rightX, leftX, "the pupil slides toward the cursor");
            Assert.Less(rightX - leftX, 40, "damped by the parallax divisor, never glued to the cursor");
            Row("eye.pupil-parallax");

            // A blink is a 1-in-161 roll each step: given time, the lid comes down.
            input.MoveMouseTo(ViewportPoint(0.5f, 0.5f));
            bool blinked = false;
            float blinkBy = Time.time + 40f;
            while (!blinked && Time.time < blinkBy)
            {
                blinked = eyeball.State.IsBlinking || eyeball.State.EyelidDrop > 0f;
                yield return null;
            }
            Assert.IsTrue(blinked, "the eyelid blinked at random");
            Row("eye.blink");

            // The cursor is an eye: right click (gun holstered) flips look/talk, the wheel does too.
            Assert.AreEqual(EyeCursorMode.Look, cursor.Mode);
            Assert.AreEqual(EyeCursorSprite.Eye, cursor.CurrentSprite);
            yield return input.RightClick();
            Assert.AreEqual(EyeCursorMode.Talk, cursor.Mode);
            Assert.AreEqual(EyeCursorSprite.Talk, cursor.CurrentSprite);
            yield return input.Wheel(1f);
            Assert.AreEqual(EyeCursorMode.Look, cursor.Mode, "the wheel cycles too");
            Assert.AreEqual(EyeCursorSprite.Eye, cursor.CurrentSprite);
            Row("cursor.look-talk-cycle");
        }

        // ---------------------------------------------------------------- Weapon

        [UnityTest]
        public IEnumerator Tour_3_the_gun_is_drawn_aimed_fired_and_holstered_and_the_HUD_pistol_does_the_same()
        {
            Room room = null;
            yield return EnterStreet(r => room = r);
            var player = room.Player;
            var weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            var cursor = Object.FindAnyObjectByType<EyeCursor>();
            var icon = Object.FindAnyObjectByType<HudPistolIcon>();
            Assert.IsNotNull(weapon);
            Assert.IsNotNull(cursor);
            Assert.IsNotNull(icon);

            Send(room, new TeleportPlayer(player.Id, 6000f));
            input.MoveMouseTo(ViewportPoint(0.5f, 0.5f));
            yield return Until(() => icon.Slide.Current == ProgramIconSlide.Mode.Idle, 10f, "the pistol icon rises");

            // Z draws it: reload sound, the arm to its drawing pose, the HUD icon selected.
            yield return input.Tap(Key.Z);
            Assert.AreEqual(PlayerWeaponState.AimMode.Equip, weapon.State.Mode);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Drawing, weapon.State.GunPose);
            Assert.IsTrue(events.OfType<WeaponSounded>().Any(e => e.Sound == PlayerWeaponState.WeaponSound.Reload), "sfx_reload");
            Assert.AreEqual("spr_icon_pistol_selected", icon.CurrentTexture.name);
            Row("weapon.draw-with-Z");

            // Hold right: the arms raise, the reticle replaces the eye, and walking is locked.
            input.HoldRight();
            yield return null;
            yield return null;
            Assert.AreEqual(PlayerWeaponState.AimMode.Aiming, weapon.State.Mode);
            Assert.IsTrue(events.OfType<WeaponSounded>().Any(e => e.Sound == PlayerWeaponState.WeaponSound.Whoosh), "sfx_whoosh");
            yield return Until(() => !weapon.State.IsRaising, 5f, "the support arm settles");
            Assert.AreEqual(PlayerWeaponState.SupportArmPose.AimHold, weapon.State.SupportPose);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Aim, weapon.State.GunPose);
            Assert.AreEqual(EyeCursorSprite.Reticle, cursor.CurrentSprite);
            Row("cursor.reticle-while-aiming");
            float aimedAt = player.State.PositionX;
            Facing aimedFacing = player.State.Facing;
            input.Hold(Key.D);
            yield return WaitGame(1f);
            input.Release(Key.D);
            Assert.AreEqual(aimedAt, player.State.PositionX, 0.01f, "aiming locks walking");
            Assert.AreEqual(aimedFacing, player.State.Facing, "and turning");
            Row("weapon.aim-locks-movement");

            // The arm points where the mouse is (mouse Y), and left click fires a bullet with smoke.
            input.MoveMouseTo(ViewportPoint(0.5f, 0.05f));
            yield return null;
            yield return null;
            yield return input.LeftDown();
            var shotHigh = events.OfType<BulletFired>().Last();
            Assert.AreEqual(1, Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Length, "one bullet per shot");
            Assert.AreEqual(1, Object.FindObjectsByType<MuzzleSmoke>(FindObjectsSortMode.None).Length, "and one puff of smoke");
            Assert.IsTrue(events.OfType<WeaponSounded>().Any(e => e.Sound == PlayerWeaponState.WeaponSound.Fire), "sfx_fire_cannon");
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Fire, weapon.State.GunPose);
            var bullet = Object.FindAnyObjectByType<Bullet>();
            Vector3 muzzle = bullet.transform.position;
            yield return Shot("aiming-firing");
            yield return input.LeftUp();
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Aim, weapon.State.GunPose, "left release returns the gun arm to aim");
            yield return null;
            yield return null;
            if (bullet != null) Assert.AreNotEqual(muzzle, bullet.transform.position, "the bullet travels");
            Row("weapon.fire-bullet-and-smoke");
            yield return Until(() => Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Length == 0, 30f, "the bullet leaves the room");
            Row("weapon.bullet-despawns");

            input.MoveMouseTo(ViewportPoint(0.5f, 0.95f));
            yield return null;
            yield return null;
            yield return input.LeftDown();
            var shotLow = events.OfType<BulletFired>().Last();
            yield return input.LeftUp();
            Assert.AreNotEqual(shotHigh.Spawn.Direction, shotLow.Spawn.Direction, "the aim angle follows the mouse's height");
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(shotHigh.Spawn.Direction, shotLow.Spawn.Direction)), 40f, "within the arm's small range");
            Row("weapon.arm-follows-mouse-height");

            // Releasing right drops back to the drawn gun; aiming is a hold, not a toggle.
            input.MoveMouseTo(ViewportPoint(0.5f, 0.5f));
            yield return input.ReleaseRight();
            Assert.AreEqual(PlayerWeaponState.AimMode.Equip, weapon.State.Mode);
            Assert.AreEqual(PlayerWeaponState.GunArmPose.Drawing, weapon.State.GunPose);
            Row("weapon.aim-is-a-hold");

            // The HUD pistol icon holsters it exactly as Z would, then draws it again.
            Vector2 iconAt = new Vector2(icon.ScreenRect.center.x, Screen.height - icon.ScreenRect.center.y);
            yield return input.Click(iconAt);
            Assert.AreEqual(PlayerWeaponState.AimMode.Off, weapon.State.Mode, "a click on the icon holstered the gun");
            Assert.IsTrue(events.OfType<WeaponSounded>().Any(e => e.Sound == PlayerWeaponState.WeaponSound.Holster), "sfx_holster");
            Assert.AreEqual("spr_icon_pistol", icon.CurrentTexture.name);
            yield return input.Click(iconAt);
            Assert.AreEqual(PlayerWeaponState.AimMode.Equip, weapon.State.Mode, "and a second click drew it");
            yield return input.Tap(Key.Z);
            Assert.AreEqual(PlayerWeaponState.AimMode.Off, weapon.State.Mode, "Z holsters it again");
            Row("hud.pistol-icon-toggles-gun");
        }

        // ---------------------------------------------------------------- NPC and the hacking console

        [UnityTest]
        public IEnumerator Tour_4_the_NPC_patrols_grows_suspicious_is_looked_at_talked_to_shot_and_hacked()
        {
            Time.timeScale = 1f;
            Room room = null;
            yield return EnterStreet(r => room = r);
            var player = room.Player;
            var npc = Object.FindAnyObjectByType<NpcController>();
            var interaction = npc.GetComponent<NpcInteraction>();
            var weapon = Object.FindAnyObjectByType<PlayerWeapon>();
            var cursor = Object.FindAnyObjectByType<EyeCursor>();
            var overlay = Object.FindAnyObjectByType<HackingOverlay>();
            Assert.IsNotNull(npc, "street00 has its one NPC");
            yield return Until(() => npc.Entity != null, 5f, "the NPC enters the world");

            // Patrol: walks toward its waypoint at obj_npc's fixed speed.
            float npcStart = npc.Patrol.PositionX;
            yield return WaitGame(1f);
            Assert.AreEqual(NpcPatrolState.PatrolMode.Walking, npc.Patrol.CurrentMode);
            Assert.Greater(npc.Patrol.PositionX, npcStart + 50f, "the NPC walked toward its waypoint");
            Row("npc.patrols");

            // Suspicion: close, facing each other and not looking down raises it.
            input.MoveMouseTo(ViewportPoint(0.5f, 0.1f));
            yield return PlaceBesideNpc(room, npc, 900f);
            float before = npc.Suspicion.Suspicion;
            yield return WaitGame(1f);
            Assert.Greater(npc.Suspicion.Suspicion, before + 2f, "suspicion climbs near, face to face, eyes up");
            Row("npc.suspicion-rises-near-and-facing");
            yield return OnScreen(npc.GetComponent<Collider2D>());
            int shown = int.Parse(npc.SuspicionText.TrimEnd('%'));
            Assert.AreEqual(npc.Suspicion.DisplayPercent, shown, 2, "the % over the NPC's head shows it");
            Row("npc.suspicion-label");
            yield return Shot("npc-suspicion");

            // Looking down (a console open) cancels the face-to-face bonus.
            input.MoveMouseTo(ViewportPoint(0.5f, 0.9f));
            yield return PlaceBesideNpc(room, npc, 500f);
            yield return null;
            before = npc.Suspicion.Suspicion;
            yield return WaitGame(0.7f);
            Assert.Less(npc.Suspicion.Suspicion - before, 0.5f, "looking down stops it growing");
            Row("npc.looking-down-stops-suspicion");

            // Far away it bleeds off.
            before = npc.Suspicion.Suspicion;
            Assert.Greater(before, 1f, "there is something to lose");
            Send(room, new TeleportPlayer(player.Id, Mathf.Min(npc.Patrol.PositionX + 4000f, StreetWidth - 600f)));
            yield return WaitGame(1.5f);
            Assert.Less(npc.Suspicion.Suspicion, before, "out of sight it drains");
            Row("npc.far-bleeds-suspicion");

            // Look mode: a click types the NPC's description at the cursor.
            input.MoveMouseTo(ViewportPoint(0.5f, 0.5f));
            yield return PlaceBesideNpc(room, npc, 700f);
            yield return OnScreen(npc.GetComponent<Collider2D>());
            Assert.AreEqual(EyeCursorMode.Look, cursor.Mode);
            yield return ClickCollider(npc.GetComponent<Collider2D>());
            yield return Until(() => Object.FindObjectsByType<DescriptionTypewriter>(FindObjectsSortMode.None)
                .Any(t => t.State != null && t.State.Text.Contains("young woman")), 5f, "the NPC's description is typed");
            Row("npc.look-types-description");
            yield return WaitGame(1f);
            yield return Shot("npc-description");

            // Talk mode: the NPC is sent over to stand with the player.
            yield return input.RightClick();
            Assert.AreEqual(EyeCursorMode.Talk, cursor.Mode);
            yield return PlaceBesideNpc(room, npc, 700f);
            yield return OnScreen(npc.GetComponent<Collider2D>());
            yield return ClickCollider(npc.GetComponent<Collider2D>());
            yield return null;
            yield return null;
            Assert.AreEqual(NpcPatrolState.PatrolMode.Talking, npc.Patrol.CurrentMode, "a talk click sets the NPC talking");
            Assert.IsTrue(npc.Patrol.TalkTarget.HasValue, "toward a point on the player's far side");
            Row("npc.talk-walks-to-the-player");
            yield return input.RightClick();
            Assert.AreEqual(EyeCursorMode.Look, cursor.Mode);

            // The console: Q toggles inverted, pointer mode, the dashboard backdrop fades in.
            yield return PlaceBesideNpc(room, npc, 600f);
            yield return OnScreen(npc.GetComponent<Collider2D>());
            yield return input.Tap(Key.Q);
            Assert.IsTrue(overlay.Session.Inverted, "Q turned the console view on");
            Assert.AreEqual(EyeCursorMode.Pointer, cursor.Mode, "the only way into pointer mode");
            yield return Until(() => overlay.Session.Backdrop.Alpha > 0.1f, 5f, "the circuit backdrop fades in");
            Row("console.q-toggles-inverted");

            // In the console view obj_npc draws an inverted-blend copy of itself and spr_brain_select
            // over it, and over_gui swaps obj_mouse's eye cursor for obj_mouse_gui's GUI arrow.
            Assert.IsTrue(npc.InvertedCopyShowing, "the NPC's inverted-blend copy is drawn");
            Assert.IsTrue(npc.BrainSelectShowing, "spr_brain_select is drawn over the NPC");
            Assert.AreEqual("spr_brain_select", npc.BrainSelectRenderer.sprite.name);
            Row("console.brain-select-over-the-npc");
            Assert.IsTrue(overlay.Session.OverGui, "Q set over_gui");
            Assert.IsTrue(overlay.ArrowShowing, "the GUI arrow is drawn");
            Assert.IsFalse(cursor.Visible, "and the eye cursor is not");
            yield return Shot("console-view");

            // A pointer click on the NPC opens the program and the typed console on it.
            yield return ClickCollider(npc.GetComponent<Collider2D>());
            yield return Until(() => overlay.Session.Target == interaction, 5f, "the NPC is the hack target");
            yield return Until(() => overlay.Prompt.IsShowing && overlay.Prompt.Slide.Current == CommandPromptSlide.Mode.Idle, 5f, "the console slides in");
            Assert.IsTrue(overlay.Session.Program.Visible, "the program icon rose");
            Row("console.pointer-click-opens-the-hack");
            // obj_mouse_gui's press chain: a press on no GUI element (the NPC) cleared over_gui.
            Assert.IsFalse(overlay.Session.OverGui, "a press on the world cleared over_gui");
            Assert.IsFalse(overlay.ArrowShowing, "so the arrow is gone");
            Assert.IsTrue(cursor.Visible, "and the eye cursor is back");
            yield return Shot("console-open");

            // The typed commands.
            overlay.Prompt.Submit("help");
            yield return null;
            Assert.That(overlay.Prompt.Output, Does.Contain("List objects in vicinity"), "help");
            overlay.Prompt.Submit("list sheep");
            yield return null;
            Assert.That(overlay.Prompt.Output, Does.Contain("0097"), "list sheep names the NPC's sheep id");
            yield return Shot("console-list-sheep");
            overlay.Prompt.Submit("bogus");
            yield return null;
            Assert.AreEqual("Command not recognized", overlay.Prompt.Output);
            overlay.Prompt.Submit("clear");
            yield return null;
            Assert.IsEmpty(overlay.Prompt.Output, "clear empties the output");
            Row("console.help-list-clear-unknown");

            // A click on the console is the GUI's: it flicks, it does not reach the world.
            int flicks = overlay.FlickCount;
            overlay.GuiClicked(GuiClick.Console);
            Assert.AreEqual(flicks + 1, overlay.FlickCount, "sfx_flick");
            Row("audio.flick");
            yield return null; // the overlay's Update picks the cursor...
            yield return null; // ...and the eye cursor's LateUpdate applies it
            Assert.IsTrue(overlay.Session.OverGui, "a console press sets over_gui");
            Assert.IsTrue(overlay.ArrowShowing, "and brings the arrow back");
            Assert.IsFalse(cursor.Visible);
            Row("cursor.gui-arrow-while-over-gui");

            // Escape closes the console and ends the hack; Q restores the cursor mode.
            yield return input.Tap(Key.Escape);
            yield return Until(() => !overlay.Prompt.IsShowing, 5f, "Escape closes the console");
            Assert.IsNull(overlay.Session.Target, "closing the console ends the hack");
            Row("console.escape-closes-the-hack");
            yield return Until(() => !CommandPrompt.CapturesKeyboard, 5f, "the console has slid out and gives the keyboard back");
            yield return input.Tap(Key.Q);
            Assert.IsFalse(overlay.Session.Inverted);
            Assert.AreEqual(EyeCursorMode.Look, cursor.Mode, "the cursor mode before the console came back");
            Row("console.q-restores-the-cursor-mode");
            yield return null;
            Assert.IsFalse(npc.BrainSelectShowing, "the brain overlay goes with the console view");
            Assert.IsFalse(npc.InvertedCopyShowing);
            Assert.IsFalse(overlay.ArrowShowing, "Q off cleared over_gui");
            Assert.IsTrue(cursor.Visible);

            // obj_gui_menu: once the console backdrop is gone, with the gun holstered, the mouse over
            // the bottom 86px fades spr_stealthwall in, and moving it up fades it back out.
            yield return Until(() => overlay.Session.Backdrop.Current == OverlayFade.Mode.Off, 5f, "the circuit backdrop fades out");
            Assert.AreEqual(0f, overlay.Dashboard.Alpha, "no dashboard while the mouse is up");
            input.MoveMouseTo(ViewportPoint(0.5f, 0.995f));
            yield return Until(() => overlay.Dashboard.Alpha >= 1f, 5f, "the dashboard fades in over the bottom strip");
            yield return Shot("dashboard");
            input.MoveMouseTo(ViewportPoint(0.5f, 0.5f));
            yield return Until(() => overlay.Dashboard.Fade.Current == OverlayFade.Mode.Off, 5f, "the dashboard fades back out");
            Row("hud.dashboard-fades-over-the-bottom-strip");
        }

        [UnityTest]
        public IEnumerator Tour_5_a_shot_shoves_the_NPC_a_hundred_pixels()
        {
            Time.timeScale = 1f;
            Room room = null;
            yield return EnterStreet(r => room = r);
            var player = room.Player;
            var npc = Object.FindAnyObjectByType<NpcController>();
            yield return Until(() => npc.Entity != null, 5f, "the NPC enters the world");

            input.MoveMouseTo(ViewportPoint(0.5f, 0.5f));
            yield return PlaceBesideNpc(room, npc, 1100f);
            yield return input.Tap(Key.Z);
            input.HoldRight();
            yield return Until(() => !Object.FindAnyObjectByType<PlayerWeapon>().State.IsRaising, 5f, "the support arm settles");

            for (int attempt = 0; attempt < 3 && !events.OfType<NpcShoved>().Any(); attempt++)
            {
                yield return input.LeftDown();
                yield return input.LeftUp();
                yield return WaitGame(1.5f);
            }

            Assert.IsTrue(events.OfType<NpcShoved>().Any(), "a bullet that meets the NPC shoves it");
            Assert.AreEqual(100f, Mathf.Abs(events.OfType<NpcShoved>().First().Distance), 0.01f, "by 100px, away from the shot");
            Row("weapon.shot-shoves-the-npc");
            yield return input.ReleaseRight();
            yield return input.Tap(Key.Z);
        }

        // ---------------------------------------------------------------- Doors, exits and rooms

        [UnityTest]
        public IEnumerator Tour_6_Bleecher_Street_2nd_Avenue_and_the_shop_connect_as_the_original_did()
        {
            Room room = null;
            yield return EnterStreet(r => room = r);
            var door = Object.FindAnyObjectByType<DoorExit>();
            Assert.IsNotNull(door, "street00 has its obj_exit to the shop");

            // Right edge of street00 -> street01, entering from the left.
            Send(room, new TeleportPlayer(room.Player.Id, StreetWidth - 1500f));
            yield return WalkUntilRoomChanges(room, Key.D, "Street01", r => room = r);
            AssertArrival(room, "2nd Avenue", PlayerWidth, Facing.Right);
            Assert.IsNotNull(Object.FindAnyObjectByType<RainEmitter>(), "2nd Avenue has rain");
            Assert.That(MusicPlayer.Current.name, Does.Contain("oppressivegloom"));
            yield return WaitGame(1.5f);
            yield return Shot("2nd-avenue");
            Row("world.street00-right-exit-to-street01");

            // Left edge of street01 -> street00, entering from the right.
            Time.timeScale = 4f;
            yield return WalkUntilRoomChanges(room, Key.A, "Street00", r => room = r);
            AssertArrival(room, "Bleecher Street", StreetWidth - PlayerWidth, Facing.Left);
            Row("world.street01-left-exit-to-street00");

            // The door: W does nothing beyond 300px, enters the shop within it.
            Send(room, new TeleportPlayer(room.Player.Id, 9504f - 400f));
            yield return null;
            yield return input.Tap(Key.W);
            yield return WaitGame(1f);
            Assert.AreEqual("Street00", SceneManager.GetActiveScene().name, "W beyond 300px does nothing");
            Send(room, new TeleportPlayer(room.Player.Id, 9504f - 250f));
            yield return null;
            yield return input.Tap(Key.W);
            yield return Arrive("Shop", r => room = r);
            AttachSession();
            AssertArrival(room, "Simon's Pawn and Loan", 5760f - PlayerWidth, Facing.Left);
            Assert.That(MusicPlayer.Current.name, Does.Contain("dubferal"), "the shop's own track");
            Assert.IsNull(Object.FindAnyObjectByType<RainEmitter>(), "it is dry indoors");
            Row("world.door-enters-the-shop");
            Row("audio.shop-music");
            yield return WaitGame(1.5f);
            yield return Shot("shop");

            // The barrier at x 800 stops the player walking west.
            Send(room, new TeleportPlayer(room.Player.Id, 1400f));
            yield return null;
            input.Hold(Key.A);
            yield return WaitGame(4f);
            input.Release(Key.A);
            Assert.AreEqual(800f + RoomExitRules.StripWidth + PlayerWidth / 2f, room.Player.State.PositionX, 1f, "the barrier holds the player's box on its east side");
            Assert.AreEqual("Shop", SceneManager.GetActiveScene().name);
            yield return Shot("shop-barrier");
            Row("world.shop-barrier-blocks");

            // The shop's right edge -> street00 at x 9500.
            Send(room, new TeleportPlayer(room.Player.Id, 5200f));
            yield return null;
            yield return WalkUntilRoomChanges(room, Key.D, "Street00", r => room = r);
            AssertArrival(room, "Bleecher Street", 9500f, Facing.Right);
            Row("world.shop-exit-to-street00");

            // Left edge of street00 -> street01, entering from the right; and its right edge back.
            Send(room, new TeleportPlayer(room.Player.Id, 1500f));
            yield return null;
            yield return WalkUntilRoomChanges(room, Key.A, "Street01", r => room = r);
            AssertArrival(room, "2nd Avenue", 15360f - PlayerWidth, Facing.Left);
            Row("world.street00-left-exit-to-street01");
            yield return WalkUntilRoomChanges(room, Key.D, "Street00", r => room = r);
            AssertArrival(room, "Bleecher Street", PlayerWidth, Facing.Right);
            Row("world.street01-right-exit-to-street00");
        }

        // ---------------------------------------------------------------- Helpers

        /// <summary>One proof of the parity checklist (docs/RECONSTRUCTION.md section 11).</summary>
        private static void Row(string id) => Passed.Add(id);

        private void AttachSession()
        {
            if (session != null) session.Played -= events.Add;
            session = Object.FindAnyObjectByType<GameSession>();
            Assert.IsNotNull(session, "the room has its GameSession");
            session.Played += events.Add;
        }

        private IEnumerator EnterStreet(Action<Room> found)
        {
            SceneManager.LoadScene("Titlescreen");
            yield return null;
            yield return null;
            yield return input.Tap(Key.Space);
            Room room = null;
            yield return Arrive("Street00", r => room = r);
            AttachSession();
            input.MoveMouseTo(ViewportPoint(0.5f, 0.5f));
            found(room);
        }

        private static bool Send(Room room, GameCommand command) => room.Player.Send(command);

        private static void AssertArrival(Room room, string name, float x, Facing facing)
        {
            Assert.AreEqual(name, room.DisplayName);
            Assert.AreEqual(x, room.Player.State.PositionX, 1f, $"{name}: entry x");
            Assert.AreEqual(facing, room.Player.State.Facing, $"{name}: entry facing");
            Assert.IsFalse(room.Player.AutoWalk, $"{name}: autowalk is off on arrival");
        }

        private static bool Settled(CloseFramingCamera camera)
        {
            float x = camera.State.CenterX;
            return Mathf.Abs(x - camera.State.TargetX(camera.Player.State.PositionX, camera.Player.State.Facing)) < 1f
                || x >= StreetWidth - 960f - 1f || x <= 960f + 1f;
        }

        /// <summary>Moves the player beside the NPC (offset px to its right) facing it.</summary>
        private IEnumerator PlaceBesideNpc(Room room, NpcController npc, float offset)
        {
            var player = room.Player;
            while (player.State.CurrentMode != PlayerLocomotionState.WalkMode.Idle) yield return null;
            Send(room, new TeleportPlayer(player.Id, npc.Patrol.PositionX + offset));
            Send(room, new TurnPlayer(player.Id, Facing.Left));
            yield return null;
        }

        private static Vector2 ViewportPoint(float x, float y) => new Vector2(Screen.width * x, Screen.height * (1f - y));

        private static Vector2 ScreenOf(Collider2D collider)
        {
            Vector3 p = Camera.main.WorldToScreenPoint(collider.bounds.center);
            return new Vector2(p.x, p.y);
        }

        private static IEnumerator OnScreen(Collider2D collider)
        {
            yield return Until(() =>
            {
                Vector2 p = ScreenOf(collider);
                return p.x > Screen.width * 0.1f && p.x < Screen.width * 0.9f && p.y > Screen.height * 0.1f && p.y < Screen.height * 0.9f;
            }, 20f, "the camera frames " + collider.name);
            yield return CameraStill();
        }

        /// <summary>The view's own pan has stopped: the camera's height follows the mouse, so a click
        /// target is only where it was measured once the camera has settled.</summary>
        private static IEnumerator CameraStill()
        {
            Vector3 last = Camera.main.transform.position;
            int still = 0;
            for (int frames = 0; still < 5 && frames < 600; frames++)
            {
                yield return null;
                Vector3 now = Camera.main.transform.position;
                still = now == last ? still + 1 : 0;
                last = now;
            }
        }

        /// <summary>Moves the mouse onto the collider (letting the mouse-driven camera settle each time), then clicks it.</summary>
        private IEnumerator ClickCollider(Collider2D collider)
        {
            for (int i = 0; i < 4; i++)
            {
                input.MoveMouseTo(ScreenOf(collider));
                yield return CameraStill();
            }

            yield return input.Click(ScreenOf(collider));
        }

        private static IEnumerator Until(Func<bool> condition, float seconds, string what)
        {
            float waited = 0f;
            while (!condition())
            {
                waited += Time.unscaledDeltaTime;
                if (waited > seconds) Assert.Fail("timed out waiting for: " + what);
                yield return null;
            }
        }

        private static IEnumerator WaitGame(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until) yield return null;
        }

        private static IEnumerator Arrive(string scene, Action<Room> found)
        {
            float waited = 0f;
            while (SceneManager.GetActiveScene().name != scene || Object.FindAnyObjectByType<Room>() == null)
            {
                waited += Time.unscaledDeltaTime;
                if (waited > Timeout) Assert.Fail($"never reached {scene} (still in {SceneManager.GetActiveScene().name})");
                yield return null;
            }

            yield return null; // Room.Start places the player
            yield return null;
            found(Object.FindAnyObjectByType<Room>());
        }

        /// <summary>Holds a walk key until the side exit's own autowalk and fade move to another room.</summary>
        private IEnumerator WalkUntilRoomChanges(Room room, Key key, string scene, Action<Room> found)
        {
            while (room.Player.State.CurrentMode != PlayerLocomotionState.WalkMode.Idle) yield return null;
            int exits = events.Count(e => e is SideExitReached);
            input.Hold(key);
            yield return Until(() => events.Count(e => e is SideExitReached) > exits, Timeout, "the player reaches the room's exit");
            input.Release(key); // the exit's own autowalk carries them on, as in the original
            yield return Arrive(scene, found);
            AttachSession();
        }

        /// <summary>
        /// A screenshot through MindAttic.Media.Capture (on only when the run enabled it) and, when the run
        /// has a graphics device, a PNG in Logs/parity-tour: the camera and the HUD panel render
        /// into one 1920x1080 texture for a frame (batch mode never reaches WaitForEndOfFrame, so
        /// ScreenCapture cannot be used).
        /// </summary>
        private static IEnumerator Shot(string tag)
        {
            CaptureUnity.TakeScreenshot("parity-" + tag);
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            Camera cam = Camera.main;
            UIDocument hud = Object.FindAnyObjectByType<UIDocument>();
            if (cam == null || hud == null) yield break;

            var target = new RenderTexture(1920, 1080, 24);
            PanelSettings panel = hud.panelSettings;
            bool clear = panel.clearColor;
            cam.targetTexture = target;
            panel.targetTexture = target;
            panel.clearColor = false;
            yield return null;
            yield return null;

            var shot = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            shot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            cam.targetTexture = null;
            panel.targetTexture = null;
            panel.clearColor = clear;

            string dir = Path.Combine(Application.dataPath, "..", "Logs", "parity-tour");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, $"{++shotNumber:00}-{tag}.png"), shot.EncodeToPNG());
            Object.Destroy(shot);
            target.Release();
            Object.Destroy(target);
        }

        /// <summary>
        /// A virtual keyboard and mouse: state events queued into the Input System, so the game's own
        /// <c>GameInput.Poll</c> reads them as it reads hardware. Keys and buttons last until released.
        /// </summary>
        private sealed class TourInput : IDisposable
        {
            private readonly InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
            private readonly InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
#endif
            private readonly Keyboard keyboard;
            private readonly Mouse mouse;
            private readonly HashSet<Key> held = new HashSet<Key>();
            private Vector2 position;
            private bool left;
            private bool right;

            public TourInput()
            {
                previousBackground = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
                previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
                // A batch-mode Editor never has the Game view focused; the tour's input is the game's input.
                InputSystem.settings.editorInputBehaviorInPlayMode =
                    InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard = InputSystem.AddDevice<Keyboard>("ParityTourKeyboard");
                mouse = InputSystem.AddDevice<Mouse>("ParityTourMouse");
                position = new Vector2(Screen.width / 2f, Screen.height / 2f);
                Flush();
            }

            public void Dispose()
            {
                held.Clear();
                left = right = false;
                Flush();
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
                // The settings are the project's own object: leave them as found for every later test.
                InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
#endif
            }

            private void Flush(float wheel = 0f)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(held.ToArray()));
                var state = new MouseState { position = position, scroll = new Vector2(0f, wheel) };
                if (left) state = state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);
                if (right) state = state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right);
                InputSystem.QueueStateEvent(mouse, state);
            }

            public void Hold(Key key)
            {
                held.Add(key);
                Flush();
            }

            public void Release(Key key)
            {
                held.Remove(key);
                Flush();
            }

            public IEnumerator Tap(Key key)
            {
                Hold(key);
                yield return null;
                yield return null;
                Release(key);
                yield return null;
                yield return null;
            }

            public void MoveMouseTo(Vector2 screen)
            {
                position = screen;
                Flush();
            }

            public IEnumerator Click(Vector2 screen)
            {
                MoveMouseTo(screen);
                yield return null;
                yield return null;
                yield return LeftDown();
                yield return LeftUp();
            }

            public IEnumerator LeftDown()
            {
                left = true;
                Flush();
                yield return null;
                yield return null;
            }

            public IEnumerator LeftUp()
            {
                left = false;
                Flush();
                yield return null;
                yield return null;
            }

            public void HoldRight()
            {
                right = true;
                Flush();
            }

            public IEnumerator ReleaseRight()
            {
                right = false;
                Flush();
                yield return null;
                yield return null;
            }

            public IEnumerator RightClick()
            {
                right = true;
                Flush();
                yield return null;
                yield return null;
                right = false;
                Flush();
                yield return null;
                yield return null;
            }

            public IEnumerator Wheel(float delta)
            {
                Flush(delta);
                yield return null;
                yield return null;
                Flush();
                yield return null;
            }
        }
    }
}
