using Incognito.Audio;
using Incognito.Controls;
using Incognito.Display;
using Incognito.Player;
using Unity.Shared.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Incognito.World
{
    /// <summary>
    /// obj_titlescreen + the title's obj_music: any key or the left mouse button runs
    /// start_game() — straight to Bleecher Street (street00), entering from the right edge
    /// (globals: next_room = street00, enter_from = "right") with the street fading in from black.
    /// Escape and F1 are <see cref="DebugHotkeys"/>' (every room). The title plays the global default track,
    /// music_oppressivegloom (its obj_music's whispers line was commented out).
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        [SerializeField] private string firstRoom = "Street00";
        [SerializeField] private AudioClip music;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 1f;

        private bool started;

        public void Configure(string firstRoom, AudioClip music)
        {
            this.firstRoom = firstRoom;
            this.music = music;
        }

        private void Start()
        {
            // obj_titlescreen's Create (and obj_startup's before it): "Setup Screen Ratio", then
            // window_set_fullscreen(true) — the letterboxing itself is GameDisplay's, every frame.
            GameDisplay.EnterFullscreen();
            MusicPlayer.Play(music, musicVolume);

            // --headless (U.INC-T54): nobody can press a key at the title, so go straight into the first room.
            Headless.WhenActive(() => { if (this != null && !started) StartGame(); });
        }

        private void Update()
        {
            if (started) return;

            if (GameInput.Current.AnyKeyDown) StartGame();
        }

        /// <summary>start_game(): the player is created facing left, then room_goto(next_room).</summary>
        public void StartGame()
        {
            started = true;
            RoomTransition.Request(firstRoom, RoomEntrance.Right, 0f, PlayerLocomotionState.FacingDirection.Left);
            SceneManager.LoadScene(firstRoom);
        }
    }
}
