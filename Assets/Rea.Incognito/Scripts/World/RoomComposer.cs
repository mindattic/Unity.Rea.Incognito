using System;
using Incognito.Audio;
using Incognito.CameraRig;
using Incognito.Content;
using Incognito.Eye;
using Incognito.Hacking;
using Incognito.Player;
using Incognito.Settings;
using Incognito.Terminal;
using Incognito.Weapon;
using UnityEngine;

namespace Incognito.World
{
    /// <summary>
    /// Builds one room instance's data-driven content (RECONSTRUCTION.md §7–8) from its
    /// <see cref="RoomDefinition"/> and a bundle of already-resolved art/audio
    /// (<see cref="RoomRuntimeAssets"/>): the background slices, lampposts and mailboxes, rain, and
    /// side/door exits and barriers, plus the per-room wiring the sandbox rig doesn't carry (camera
    /// bounds, footstep/weapon/console audio). <see cref="RoomBootstrap"/> is the only caller, at
    /// Awake, so a generated scene and a running game build the room the exact same way (U.INC-T26)
    /// — this class has no <c>UnityEditor</c> dependency, so it is equally usable from Play mode, a
    /// build, or an Editor script, even though <see cref="RoomBootstrap"/> itself only calls it at
    /// Awake (Play mode and builds; see that type's own doc comment).
    /// </summary>
    public static class RoomComposer
    {
        private const float Px = PlayerLocomotion.WorldUnitsPerOriginalPixel;
        private const float ViewWidth = 1920f;
        private const float ViewHeight = 1080f;
        private const float PlayerOriginAboveFloor = 512f; // obj_walker initialY = room_height - sprite_height/2

        // Sorting orders/layers: mirrors RoomSceneBuilder's original depths (RECONSTRUCTION.md §7, U.INC-T24).
        private const int ExitOrder = -19;
        private const int LamppostOrder = -10;
        private const int LamplightOrder = -9;
        private const int RainOrder = 2;
        private const int FrontPropOrder = 10;
        private const int BackgroundOrder = -100;

        private const string BackgroundLayer = "Background";
        private const string BackPropLayer = "BackProps";
        private const string EffectsLayer = "Effects";
        private const string FrontPropLayer = "FrontProps";

        /// <summary>Builds <paramref name="spec"/>'s content into the active scene, wiring the
        /// sandbox rig's <paramref name="player"/>/<paramref name="framing"/>/<paramref name="cam"/>/
        /// <paramref name="cursor"/>/<paramref name="weapon"/>/<paramref name="prompt"/>/<paramref name="hacking"/>
        /// (any may be null if the rig doesn't have one).</summary>
        public static void Compose(RoomDefinition spec, RoomRuntimeAssets assets, PlayerLocomotion player,
            CloseFramingCamera framing, Camera cam, EyeCursor cursor, PlayerWeapon weapon, CommandPrompt prompt,
            HackingOverlay hacking = null)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (assets == null) throw new ArgumentNullException(nameof(assets));
            if (player == null) throw new InvalidOperationException("RoomComposer needs a PlayerLocomotion already in the scene.");

            // RoomRuntimeAssets is baked per room, indexed against spec.BackgroundSlices, by the
            // editor builder; if game-content.json has since added, removed or reordered slices
            // without rerunning it, a silent mismatch here would render a blank tile or play the
            // wrong music instead of the room this spec describes.
            if (assets.BackgroundSlices.Length != spec.BackgroundSlices.Length)
            {
                throw new InvalidOperationException(
                    $"Room '{spec.Id}' has {spec.BackgroundSlices.Length} background slices in game-content.json " +
                    $"but {assets.BackgroundSlices.Length} baked art references — rerun Incognito/Build Room Scenes.");
            }
            for (int i = 0; i < assets.BackgroundSlices.Length; i++)
            {
                if (assets.BackgroundSlices[i] == null)
                {
                    throw new InvalidOperationException(
                        $"Room '{spec.Id}'s background slice {i} ('{spec.BackgroundSlices[i]}') has no baked sprite " +
                        "— rerun Incognito/Build Room Scenes.");
                }
            }
            if (!string.IsNullOrEmpty(spec.Music) && assets.Music == null)
            {
                throw new InvalidOperationException(
                    $"Room '{spec.Id}' names music track '{spec.Music}' in game-content.json but has no baked clip " +
                    "— rerun Incognito/Build Room Scenes.");
            }

            // Player at its room start; the camera rests on the room's full 1080px height.
            player.transform.position = new Vector3(spec.PlayerStartX * Px, PlayerOriginAboveFloor * Px, player.transform.position.z);
            if (cam != null)
            {
                cam.transform.position = new Vector3(spec.PlayerStartX * Px, ViewHeight * 0.5f * Px, cam.transform.position.z);
                cam.backgroundColor = Color.black; // street00's bg_black
                if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            }
            if (framing != null) framing.Configure(0f, spec.Width * Px);

            // Audio: footsteps + whoosh (obj_walker), weapon sounds (toggle_gun / shoot_bullet),
            // keystrokes (obj_command_prompt) and the program-icon click (obj_mouse_gui).
            var playerAudioSource = GetOrAdd<AudioSource>(player.gameObject);
            playerAudioSource.playOnAwake = false;
            var playerAudio = GetOrAdd<PlayerAudio>(player.gameObject);
            playerAudio.Configure(playerAudioSource, assets.Footsteps, assets.Whoosh);

            if (weapon != null)
            {
                weapon.Configure(assets.ReloadClip, assets.HolsterClip, assets.Whoosh, assets.FireClip,
                    new Rect(0f, 0f, spec.Width * Px, ViewHeight * Px));
            }

            if (prompt != null)
            {
                var typeSource = GetOrAdd<AudioSource>(prompt.gameObject);
                typeSource.playOnAwake = false;
                prompt.ConfigureAudio(typeSource, assets.TypeClip);
            }

            // obj_mouse_gui: sfx_flick on a program-icon click (U.INC-T28).
            if (hacking != null)
            {
                if (assets.FlickClip == null)
                {
                    throw new InvalidOperationException(
                        $"Room '{spec.Id}' has a hacking overlay but no baked sfx_flick clip — rerun Incognito/Build Room Scenes.");
                }
                var flickSource = GetOrAdd<AudioSource>(hacking.gameObject);
                flickSource.playOnAwake = false;
                hacking.ConfigureAudio(flickSource, assets.FlickClip);
            }

            // The room: background slices (tiles at depth 1000000), left to right.
            var backdrop = new GameObject("Background");
            for (int i = 0; i < spec.BackgroundSlices.Length; i++)
            {
                var slice = new GameObject(spec.BackgroundSlices[i], typeof(SpriteRenderer));
                slice.transform.SetParent(backdrop.transform, false);
                var sr = slice.GetComponent<SpriteRenderer>();
                sr.sprite = assets.BackgroundSlices[i]; // lengths checked equal, and every slot non-null, above
                sr.sortingLayerName = BackgroundLayer;
                sr.sortingOrder = BackgroundOrder;
                slice.transform.position = GmToWorld(i * ViewWidth, 0f);
            }

            // obj_room + obj_music.
            var fade = new GameObject("Fade").AddComponent<RoomFade>();
            var room = new GameObject("Room").AddComponent<Room>();
            room.Configure(spec.DisplayName, spec.Width, GameSettings.Tuning.Camera.PlayerWidth, player,
                assets.Music, 1f, spec.OpeningText ?? string.Empty, new Vector2(spec.OpeningTextX, spec.OpeningTextY),
                assets.DescriptionBorder, cam);

            // Props: lampposts (behind, with their flickering pooled light) and mailboxes (in front).
            var props = new GameObject("Props");
            foreach (PropSpot l in spec.Lampposts)
            {
                var post = CreateProp(props.transform, "Lamppost", assets.LamppostSprite, BackPropLayer, LamppostOrder, l.X, 0f,
                    l.Description, cursor, cam, assets.DescriptionBorder);
                var glow = new GameObject("Lamplight", typeof(SpriteRenderer));
                glow.transform.SetParent(post.transform, false);
                glow.transform.position = GmToWorld(l.X, -100f);
                var glowSr = glow.GetComponent<SpriteRenderer>();
                glowSr.sprite = assets.LamplightSprite;
                glowSr.sortingLayerName = BackPropLayer;
                glowSr.sortingOrder = LamplightOrder;
                post.AddComponent<LampFlicker>().Configure(glowSr);
            }
            foreach (PropSpot m in spec.Mailboxes)
            {
                CreateProp(props.transform, "Mailbox", assets.MailboxSprite, FrontPropLayer, FrontPropOrder, m.X, 800f,
                    m.Description, cursor, cam, assets.DescriptionBorder);
            }

            if (spec.Rain)
            {
                new GameObject("Rain").AddComponent<RainEmitter>().Configure(assets.RainSprite, cam, EffectsLayer, RainOrder);
            }

            // Exits.
            var exits = new GameObject("Exits");
            foreach (SideExitSpec e in spec.SideExits)
            {
                var go = new GameObject($"Exit Side x={e.X:0} -> {e.To}");
                go.transform.SetParent(exits.transform, false);
                go.AddComponent<SideExit>().Configure(e.X, e.To, Convert(e.Enter), e.EnterX, room, fade);
            }
            foreach (DoorSpec d in spec.Doors)
            {
                var go = new GameObject($"Exit {d.Label} -> {d.To}", typeof(SpriteRenderer));
                go.transform.SetParent(exits.transform, false);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = assets.ExitIconSprite;
                sr.sortingLayerName = BackPropLayer;
                sr.sortingOrder = ExitOrder;
                sr.color = new Color(1f, 1f, 1f, 0f);
                var doorPosition = new Vector2(d.X, d.Y);
                go.transform.position = GmToWorld(d.X, d.Y);
                go.AddComponent<DoorExit>().Configure(doorPosition, d.Label, d.To, Convert(d.Enter), d.EnterX, room, fade,
                    playerAudioSource, assets.DoorOpenClip, cam);
            }
            foreach (float x in spec.Barriers)
            {
                var go = new GameObject($"Barrier x={x:0}");
                go.transform.SetParent(exits.transform, false);
                go.AddComponent<RoomBarrier>().Configure(x, room);
            }
        }

        /// <summary>RoomEntranceKind (Core, data) -> RoomEntrance (Runtime): the same three cases,
        /// kept as separate types so room data stays engine-free (U.INC-T22).</summary>
        private static RoomEntrance Convert(RoomEntranceKind kind)
        {
            switch (kind)
            {
                case RoomEntranceKind.Left: return RoomEntrance.Left;
                case RoomEntranceKind.Right: return RoomEntrance.Right;
                case RoomEntranceKind.At: return RoomEntrance.At;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unrecognized RoomEntranceKind.");
            }
        }

        private static GameObject CreateProp(Transform parent, string name, Sprite sprite, string sortingLayer, int order,
            float x, float y, string description, EyeCursor cursor, Camera cam, Texture2D border)
        {
            var go = new GameObject($"{name} x={x:0}", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            go.transform.position = GmToWorld(x, y);
            go.AddComponent<BoxCollider2D>(); // after the sprite, so it fits the sprite's bounds
            go.AddComponent<PropDescription>().Configure(description, cursor, cam, border);
            return go;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        /// <summary>A GameMaker room point (px from the top-left, y down) in world units: the
        /// room's floor (y = 1080) sits at world y = 0, as in the sandbox.</summary>
        private static Vector3 GmToWorld(float x, float y) => new Vector3(x * Px, (ViewHeight - y) * Px, 0f);
    }
}
