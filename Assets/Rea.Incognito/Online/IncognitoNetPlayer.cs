using Incognito.Controls;
using Incognito.Display;
using Incognito.Player;
using Incognito.Settings;
using Incognito.Weapon;
using Incognito.World;
using Unity.Multiplayer;      // NetEntity, NetPlayer
using Unity.Multiplayer.Core; // [ToServer], [Synced]
using UnityEngine;

namespace Incognito.Online
{
    /// <summary>
    /// An online player (U.INC-T43, rebuild standard point 9): the NetworkManager's Player Prefab, one per
    /// connection, carrying its roster seat. Its owner reads the same <see cref="GameInput"/> snapshot local
    /// play reads and sends it as [ToServer] calls, which the server turns into the same Core commands
    /// <see cref="PlayerLocomotion"/> and <see cref="PlayerWeapon"/> send locally (SetControls, ToggleGun,
    /// PressAim, ReleaseAim, PressFire, ReleaseFire), each carrying this seat's <see cref="PlayerId"/>, so a
    /// client only ever moves its own player. The server's <see cref="IncognitoAuthority"/> runs them on the
    /// one <see cref="GameWorld"/>, steps it, and writes this seat's state into the [Synced] fields below,
    /// which every peer (late joiners too) draws. Nothing on a client changes the game. The name and colour
    /// are not copied here: they come from the session's roster.
    /// </summary>
    public partial class IncognitoNetPlayer : NetEntity
    {
        public const string PrefabResource = "Incognito/Online/IncognitoNetPlayer";

        /// <summary>Most control updates a second the server takes from one player.</summary>
        public const int ControlsRate = 60;

        /// <summary>Original px the cursor must move before its height is sent again.</summary>
        public const float CursorResendPx = 8f;

        /// <summary>obj_walker's y: room_height - sprite_height / 2 (the body's origin, 512 original px above the floor).</summary>
        private const float BodyOriginAboveFloor = 512f;

        private const float Px = PlayerLocomotion.WorldUnitsPerOriginalPixel;

        // ---- client -> server: the owner's input ----

        /// <summary>What the player holds: <see cref="OnlineControls.PackKeys"/>, the cursor's height and the view's width (original px).</summary>
        [ToServer(RateLimit = ControlsRate, Burst = ControlsRate * 2)]
        public partial void SendControls(int keys, float mouseBelowCentre, float viewWidth);

        /// <summary>Z: draw or holster the gun.</summary>
        [ToServer(RateLimit = 10)]
        public partial void ToggleGunKey();

        /// <summary>The right mouse button went down.</summary>
        [ToServer(RateLimit = 20)]
        public partial void AimDown();

        /// <summary>The right mouse button came up.</summary>
        [ToServer(RateLimit = 20)]
        public partial void AimUp();

        /// <summary>The left mouse button went down.</summary>
        [ToServer(RateLimit = 20)]
        public partial void FireDown();

        /// <summary>The left mouse button came up.</summary>
        [ToServer(RateLimit = 20)]
        public partial void FireUp();

        // ---- server -> everyone: the state the authority owns ----

        [Synced] private int _seat = -1;
        [Synced] private float _x;
        [Synced] private float _bob;
        [Synced] private bool _facingRight;
        [Synced] private int _walkModeValue;
        [Synced] private int _aimModeValue;

        private IncognitoAuthority authority; // server only
        private PlayerId id;                  // server only

        private int sentKeys = -1;            // owner only
        private float sentCursor = float.NaN;
        private float sentViewWidth = float.NaN;
        private float lastSent = -1f;
        private SpriteRenderer body;

        /// <summary>The seat's player id (seat 0 is P1), as every peer sees it; default until the server seats it.</summary>
        public PlayerId PlayerId => Seat >= 0 ? OnlineSeats.PlayerFor(Seat) : default;

        public PlayerLocomotionState.WalkMode WalkMode => (PlayerLocomotionState.WalkMode)WalkModeValue;
        public PlayerWeaponState.AimMode AimMode => (PlayerWeaponState.AimMode)AimModeValue;
        public bool GunOut => AimMode != PlayerWeaponState.AimMode.Off;

        /// <summary>The owner's name, from the roster ("Player" until the roster has it).</summary>
        public string DisplayName => Roster(out NetPlayer player) ? player.DisplayName : "Player";

        /// <summary>The owner's colour, from the roster (their pick, or their seat's palette colour).</summary>
        public Color32 Color => Roster(out NetPlayer player) ? player.UnityColor() : new Color32(255, 255, 255, 255);

        protected override void OnSpawned()
        {
            if (IsServer)
            {
                authority = IncognitoAuthority.For(Session);
                int seat = Owner.HasSeat ? Owner.SeatIndex : 0;
                id = authority.Join(this, seat);
            }

            body = GetComponent<SpriteRenderer>();
            Draw();
        }

        protected override void OnDespawned()
        {
            if (authority != null) authority.Leave(this);
            authority = null;
        }

        /// <summary>Server: writes <paramref name="playerId"/>'s state from the world into the replicated fields.</summary>
        internal void Publish(GameWorld world, PlayerId playerId)
        {
            if (!IsServer) return;
            PlayerEntity player = world.FindPlayer(playerId);
            if (player == null) return;

            Seat = OnlineSeats.SeatOf(playerId);
            X = player.Locomotion.PositionX;
            Bob = player.Locomotion.BobOffset;
            FacingRight = player.Locomotion.Facing == PlayerLocomotionState.FacingDirection.Right;
            WalkModeValue = (int)player.Locomotion.CurrentMode;
            AimModeValue = (int)player.Weapon.Mode;
        }

        // ---- server: each call becomes the same Core command local play sends, as this seat's ----

        private void OnSendControls(int keys, float mouseBelowCentre, float viewWidth) =>
            Run(new SetControls(id, OnlineControls.Unpack(keys, mouseBelowCentre, viewWidth, GameSettings.Tuning.Weapon.ViewHeight)));

        private void OnToggleGunKey() => Run(new ToggleGun(id));
        private void OnAimDown() => Run(new PressAim(id));
        private void OnAimUp() => Run(new ReleaseAim(id));
        private void OnFireDown() => Run(new PressFire(id));
        private void OnFireUp() => Run(new ReleaseFire(id));

        private void Run(GameCommand command)
        {
            if (authority == null || authority.World.FindPlayer(id) == null) return;
            authority.Submit(command);
            Publish(authority.World, id);
        }

        /// <summary>
        /// Set by <see cref="IncognitoBot"/> on the player object it drives, so <see cref="Update"/> stops
        /// reading real input for it. <see cref="Unity.Multiplayer.NetBots.IsBot"/> only says this *process*
        /// was launched with <c>-bot &lt;role&gt;</c>; a <c>NetBotTest</c> runs every bot client in-process (no
        /// separate launch per bot), where that flag stays false for all of them, so it can't tell which
        /// owned player is bot-driven. Tracking it on the entity itself works in both a real <c>-bot</c>
        /// process and the in-process test harness (U.INC-T48).
        /// </summary>
        internal bool BotDriven { get; set; }

        private void Update()
        {
            if (!IsSpawned) return;
            if (IsOwner && !BotDriven) ReadInput(); // a bot's driver makes the calls itself (IncognitoBot)
        }

        private void LateUpdate()
        {
            if (!IsSpawned) return;
            Draw();
            if (IsOwner) Follow();
        }

        /// <summary>Owner: this frame's input, as the calls local play's views turn into commands.</summary>
        private void ReadInput()
        {
            InputSnapshot input = GameInput.Current;
            var controls = new PlayerControls(
                moveLeft: input.MoveLeftHeld,
                moveRight: input.MoveRightHeld,
                sprint: input.ShiftHeld,
                shiftHeld: input.ShiftHeld,
                mouseBelowCentre: MouseBelowCentre(),
                lookingDown: LookingDown(),
                viewWidth: ViewWidth());

            int keys = OnlineControls.PackKeys(controls);
            bool keysChanged = keys != sentKeys;
            bool cursorMoved = float.IsNaN(sentCursor) || Mathf.Abs(controls.MouseBelowCentre - sentCursor) >= CursorResendPx
                || !Mathf.Approximately(controls.ViewWidth, sentViewWidth);
            if (keysChanged || (cursorMoved && Time.unscaledTime - lastSent >= 1f / (ControlsRate / 2f)))
            {
                sentKeys = keys;
                sentCursor = controls.MouseBelowCentre;
                sentViewWidth = controls.ViewWidth;
                lastSent = Time.unscaledTime;
                SendControls(keys, controls.MouseBelowCentre, controls.ViewWidth);
            }

            if (input.DrawPressed) ToggleGunKey();
            if (input.AimPressed) AimDown();
            if (input.AimReleased) AimUp();
            if (input.LeftClickPressed) FireDown();
            if (input.FireReleased) FireUp();
        }

        // PlayerLocomotion's own readings: the cursor relative to the letterboxed view (U.INC-T32).
        private static float MouseBelowCentre()
        {
            if (Screen.height <= 0) return 0f;
            float fromTop = GameDisplay.PointerInView.y / GameDisplay.ReferenceSize.y;
            return (fromTop - 0.5f) * GameSettings.Tuning.Weapon.ViewHeight;
        }

        private static bool LookingDown() =>
            Screen.height > 0 && GameDisplay.PointerInView.y > GameDisplay.ReferenceSize.y * 0.5f;

        private static float ViewWidth()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return GameSettings.Tuning.Camera.ViewWidth;
            return cam.orthographicSize * 2f * cam.aspect / Px;
        }

        // ---- presentation: reads the replicated state, never writes it ----

        private void Draw()
        {
            transform.position = new Vector3(X * Px, (BodyOriginAboveFloor + Bob) * Px, transform.position.z);
            if (body == null) return;
            body.flipX = FacingRight; // as PlayerLocomotion draws the local player
            body.enabled = Seat >= 0;
            // Your own player looks as it does in local play; everyone else wears their roster colour.
            body.color = IsOwner ? UnityEngine.Color.white : (UnityEngine.Color)Color;
        }

        /// <summary>Owner: the camera keeps this player in view, inside the room.</summary>
        private void Follow()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            float half = cam.orthographicSize * cam.aspect;
            float roomWidth = IncognitoOnlineGame.RoomWidth * Px;
            float x = roomWidth > half * 2f ? Mathf.Clamp(X * Px, half, roomWidth - half) : roomWidth * 0.5f;
            cam.transform.position = new Vector3(x, cam.transform.position.y, cam.transform.position.z);
        }

        private bool Roster(out NetPlayer player)
        {
            player = null;
            return Session != null && Session.Roster != null && Session.Roster.TryGet(OwnerClientId, out player);
        }
    }
}
