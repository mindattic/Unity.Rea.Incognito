using Incognito.Content;
using Incognito.Controls;
using Incognito.Display;
using Incognito.Player;
using Incognito.Terminal;
using Incognito.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Incognito.World
{
    /// <summary>
    /// obj_debug (created by start_game.gml, persistent, so live in every room): Escape quits,
    /// F1 toggles fullscreen, F12 toggles a readout of the room, player x and mouse position, drawn
    /// as a UI Toolkit label (U.INC-T26 — was IMGUI). Escape closes the console first (it runs
    /// before this, so an open prompt keeps the key). Installed once, before the first scene loads,
    /// and kept across scene changes.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class DebugHotkeys : MonoBehaviour
    {
        private bool showReadout;
        private Label readoutLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("DebugHotkeys");
            DontDestroyOnLoad(host);
            host.AddComponent<DebugHotkeys>();
        }

        /// <summary>The F12 readout text.</summary>
        public static string FormatReadout(string room, float playerX, Vector2 mouse)
        {
            return $"room: {room}\nplayerX: {playerX:0}\nmouse: {mouse.x:0}, {mouse.y:0}";
        }

        private void Awake()
        {
            HudRoot.EnsureInstalled();
            readoutLabel = new Label { pickingMode = PickingMode.Ignore };
            readoutLabel.style.position = Position.Absolute;
            // obj_debug's draw-GUI: fnt_basic, draw_text_outline(100, 80, 2, ...) (U.INC-T31).
            readoutLabel.style.left = 100f;
            readoutLabel.style.top = 80f;
            OutlinedText.Outline(readoutLabel, GameFonts.Basic, 2);
            readoutLabel.style.display = DisplayStyle.None;
            HudRoot.RoomInfo.Add(readoutLabel);
        }

        private void OnDestroy()
        {
            readoutLabel?.RemoveFromHierarchy();
        }

        private void Update()
        {
            if (GameInput.Current.EscapePressed && !CommandPrompt.CapturesKeyboard) Application.Quit();
            // F1 on the title, obj_game and obj_debug alike: window_set_fullscreen(!window_get_fullscreen()),
            // between the display-sized fullscreen and the 1920x1080 window (U.INC-T32).
            if (GameInput.Current.DebugFullscreenTogglePressed) GameDisplay.ToggleFullscreen();
            if (GameInput.Current.DebugReadoutTogglePressed) showReadout = !showReadout;

            readoutLabel.style.display = showReadout ? DisplayStyle.Flex : DisplayStyle.None;
            if (!showReadout) return;

            var player = FindFirstObjectByType<PlayerLocomotion>();
            // obj_debug drew player.x, in original px (the world's state), not the transform's world units.
            float x = player != null && player.State != null ? player.State.PositionX : 0f;
            readoutLabel.text = FormatReadout(SceneManager.GetActiveScene().name, x, GameInput.PointerScreenPosition);
        }
    }
}
