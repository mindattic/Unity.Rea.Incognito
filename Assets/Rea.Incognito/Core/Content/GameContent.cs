using System;
using System.Collections.Generic;
using Incognito.Settings;
using Unity.Shared.Core.Json;

namespace Incognito.Content
{
    /// <summary>
    /// The whole game's authored content catalog (U.INC-T22): every room, every NPC and the
    /// console's text, in one validated place instead of literals spread across
    /// RoomSceneBuilder, SandboxSceneBuilder and CommandInterpreter. Loaded from
    /// <c>Resources/Data/game-content.json</c> by <c>Incognito.Settings.GameSettings</c> (Runtime,
    /// which only reads the file's bytes); parsed here with Unity.Shared.Core's engine-free JSON
    /// reader (U.INC-T25, replacing UnityEngine.JsonUtility), so this type stays engine-free and
    /// can be built, parsed and validated from a plain C# test.
    /// </summary>
    [Serializable]
    public sealed class GameContent
    {
        public RoomDefinition[] Rooms = Array.Empty<RoomDefinition>();
        public NpcDefinition[] Npcs = Array.Empty<NpcDefinition>();
        public ConsoleText Console = new ConsoleText();

        /// <summary>Every original sound the rebuild imports (U.INC-T28): room music, the title's
        /// tracks and every effect a live original object plays, looked up by id.</summary>
        public SoundDefinition[] Sounds = Array.Empty<SoundDefinition>();

        /// <summary>The title screen's audio (U.INC-T28); its names refer to <see cref="Sounds"/>.</summary>
        public TitleScreenSpec TitleScreen = new TitleScreenSpec();

        /// <summary>Every original sprite and background the rebuild imports (U.INC-T29), with its
        /// GameMaker origin, looked up by id.</summary>
        public ArtDefinition[] Art = Array.Empty<ArtDefinition>();

        /// <summary>The original's three fonts (U.INC-T31): every text element's size comes from
        /// here, looked up by id (<see cref="GameFonts"/>).</summary>
        public GameFontDefinition[] Fonts = Array.Empty<GameFontDefinition>();

        public RoomDefinition FindRoom(string id)
        {
            foreach (RoomDefinition room in Rooms)
                if (string.Equals(room.Id, id, StringComparison.OrdinalIgnoreCase))
                    return room;
            return null;
        }

        public SoundDefinition FindSound(string id)
        {
            foreach (SoundDefinition sound in Sounds)
                if (string.Equals(sound.Id, id, StringComparison.OrdinalIgnoreCase))
                    return sound;
            return null;
        }

        public ArtDefinition FindArt(string id)
        {
            foreach (ArtDefinition art in Art)
                if (string.Equals(art.Id, id, StringComparison.OrdinalIgnoreCase))
                    return art;
            return null;
        }

        public GameFontDefinition FindFont(string id)
        {
            foreach (GameFontDefinition font in Fonts)
                if (string.Equals(font.Id, id, StringComparison.OrdinalIgnoreCase))
                    return font;
            return null;
        }

        public NpcDefinition FindNpc(string id)
        {
            foreach (NpcDefinition npc in Npcs)
                if (string.Equals(npc.Id, id, StringComparison.OrdinalIgnoreCase))
                    return npc;
            return null;
        }

        /// <summary>
        /// Validates every room, NPC, sound, art and font entry, then the cross-references between them (a side exit
        /// or door naming a room that exists, an NPC's RoomId existing and its SpawnX inside that
        /// room, a room's or the title's track naming a catalogued sound, a room's background slice naming catalogued art). Throws <see cref="SettingsValidationException"/> on the first problem found.
        /// </summary>
        public void Validate()
        {
            if (Rooms == null || Rooms.Length == 0)
                throw new SettingsValidationException("GameContent needs at least one room.");

            var roomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RoomDefinition room in Rooms)
            {
                room.Validate();
                if (!roomIds.Add(room.Id))
                    throw new SettingsValidationException("Two rooms share the id '" + room.Id + "'.");
            }

            foreach (RoomDefinition room in Rooms)
            {
                foreach (SideExitSpec exit in room.SideExits)
                {
                    if (!roomIds.Contains(exit.To))
                        throw new SettingsValidationException("Room '" + room.Id + "'s side exit targets unknown room '" + exit.To + "'.");
                    CheckEnterX(room.Id, "side exit at x=" + exit.X, exit.Enter, exit.EnterX, FindRoom(exit.To));
                }
                foreach (DoorSpec door in room.Doors)
                {
                    if (!roomIds.Contains(door.To))
                        throw new SettingsValidationException("Room '" + room.Id + "'s door '" + door.Label + "' targets unknown room '" + door.To + "'.");
                    CheckEnterX(room.Id, "door '" + door.Label + "'", door.Enter, door.EnterX, FindRoom(door.To));
                }
            }

            if (Npcs == null)
                throw new SettingsValidationException("GameContent.Npcs must not be null.");

            var npcIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (NpcDefinition npc in Npcs)
            {
                npc.Validate();
                if (!npcIds.Add(npc.Id))
                    throw new SettingsValidationException("Two NPCs share the id '" + npc.Id + "'.");

                RoomDefinition room = FindRoom(npc.RoomId);
                if (room == null)
                    throw new SettingsValidationException("NPC '" + npc.Id + "' names unknown room '" + npc.RoomId + "'.");
                if (!(npc.SpawnX >= 0f && npc.SpawnX <= room.Width))
                    throw new SettingsValidationException(
                        "NPC '" + npc.Id + "'s SpawnX " + npc.SpawnX + " is outside room '" + room.Id + "' (width " + room.Width + ").");
            }

            if (Console == null)
                throw new SettingsValidationException("GameContent.Console is required.");
            Console.Validate();

            // Sounds (U.INC-T28): unique ids, and every track a room or the title names must be one.
            if (Sounds == null)
                throw new SettingsValidationException("GameContent.Sounds must not be null.");
            var soundIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SoundDefinition sound in Sounds)
            {
                sound.Validate();
                if (!soundIds.Add(sound.Id))
                    throw new SettingsValidationException("Two sounds share the id '" + sound.Id + "'.");
            }
            foreach (RoomDefinition room in Rooms)
                CheckSound(soundIds, "Room '" + room.Id + "'s Music", room.Music);
            if (TitleScreen == null)
                throw new SettingsValidationException("GameContent.TitleScreen must not be null.");
            CheckSound(soundIds, "TitleScreen.Music", TitleScreen.Music);
            CheckSound(soundIds, "TitleScreen.UnwiredTrack", TitleScreen.UnwiredTrack);

            // Art (U.INC-T29): unique ids and files, and every background slice a room names must be one.
            if (Art == null)
                throw new SettingsValidationException("GameContent.Art must not be null.");
            var artIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var artFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ArtDefinition art in Art)
            {
                art.Validate();
                if (!artIds.Add(art.Id))
                    throw new SettingsValidationException("Two art entries share the id '" + art.Id + "'.");
                if (!artFiles.Add(art.File))
                    throw new SettingsValidationException("Two art entries share the file '" + art.File + "'.");
            }
            // A content with no Art catalog at all (a hand-built test fixture) skips the slice check;
            // the shipped game-content.json's catalog is proven complete by OriginalArtParityTests.
            if (Art.Length > 0)
            {
                foreach (RoomDefinition room in Rooms)
                    foreach (string slice in room.BackgroundSlices)
                        if (!artIds.Contains(slice))
                            throw new SettingsValidationException("Room '" + room.Id + "'s background slice '" + slice + "' is not in Art.");
            }

            // Fonts (U.INC-T31): unique ids, and a catalog that has any fonts has all three the HUD
            // draws with. A content with none (a hand-built test fixture) skips the check.
            if (Fonts == null)
                throw new SettingsValidationException("GameContent.Fonts must not be null.");
            var fontIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (GameFontDefinition font in Fonts)
            {
                font.Validate();
                if (!fontIds.Add(font.Id))
                    throw new SettingsValidationException("Two fonts share the id '" + font.Id + "'.");
            }
            if (Fonts.Length > 0)
                foreach (string id in GameFonts.All)
                    if (!fontIds.Contains(id))
                        throw new SettingsValidationException("Fonts has no '" + id + "', which the HUD draws with.");
        }

        private static void CheckSound(HashSet<string> soundIds, string what, string id)
        {
            if (!string.IsNullOrEmpty(id) && !soundIds.Contains(id))
                throw new SettingsValidationException(what + " names unknown sound '" + id + "'.");
        }

        // An "At" entrance lands the player at a specific x in the destination room; a bad EnterX
        // would otherwise silently place them off the room's strip (U.INC-T22 review).
        private static void CheckEnterX(string fromRoomId, string what, RoomEntranceKind enter, float enterX, RoomDefinition destination)
        {
            if (enter != RoomEntranceKind.At || destination == null) return;
            if (!(enterX >= 0f && enterX <= destination.Width))
                throw new SettingsValidationException(
                    "Room '" + fromRoomId + "'s " + what + " enters '" + destination.Id + "' at x=" + enterX +
                    ", outside that room (width " + destination.Width + ").");
        }

        /// <summary>
        /// Parses <paramref name="json"/> (<c>game-content.json</c>'s text) with Unity.Shared.Core's
        /// <see cref="MiniJson"/>/<see cref="JsonObject"/> reader (U.INC-T25). Throws
        /// <see cref="JsonFormatException"/> on text that is not a JSON object; does not validate
        /// (callers validate separately, as <c>GameSettings.Load</c> does).
        /// </summary>
        public static GameContent FromJson(string json)
        {
            JsonObject root = JsonObject.Parse(json);
            return new GameContent
            {
                Rooms = root.GetObjectArray(nameof(Rooms), RoomDefinition.FromJson),
                Npcs = root.GetObjectArray(nameof(Npcs), NpcDefinition.FromJson),
                Console = ConsoleText.FromJson(root.GetObject(nameof(Console))),
                Sounds = root.GetObjectArray(nameof(Sounds), SoundDefinition.FromJson),
                TitleScreen = TitleScreenSpec.FromJson(root.GetObject(nameof(TitleScreen))),
                Art = root.GetObjectArray(nameof(Art), ArtDefinition.FromJson),
                Fonts = root.GetObjectArray(nameof(Fonts), GameFontDefinition.FromJson),
            };
        }
    }
}
