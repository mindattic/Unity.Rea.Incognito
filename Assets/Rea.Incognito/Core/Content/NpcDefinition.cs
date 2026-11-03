using System;
using Incognito.Settings;
using Incognito.Text;
using Unity.Shared.Core.Json;

namespace Incognito.Content
{
    /// <summary>
    /// One NPC's placement and content (RECONSTRUCTION.md §4): where it spawns, the route it
    /// patrols, its look-mode description and its console <c>sheep_id</c>. Kept as data
    /// (AGENTS.md law 2/4) instead of literals in SandboxSceneBuilder, so a later room or NPC is
    /// authored, not coded; loaded and validated from <c>Resources/Data/game-content.json</c>.
    /// </summary>
    [Serializable]
    public sealed class NpcDefinition
    {
        public string Id = string.Empty;

        /// <summary>The console's <c>sheep_id</c>, e.g. "0097" (printed as <c>SHP#0097</c>).</summary>
        public string SheepId = string.Empty;

        /// <summary>The <see cref="RoomDefinition.Id"/> this NPC spawns in.</summary>
        public string RoomId = string.Empty;

        /// <summary>Typed at a look-mode click (obj_npc's description). Authored in the original's
        /// GM8 form and expanded on load (<see cref="GmText.Expand"/>: '#' is a newline).</summary>
        public string Description = string.Empty;

        /// <summary>Original px, within its room.</summary>
        public float SpawnX;

        /// <summary>X positions walked in order, then looped. One waypoint = walk there and stop.</summary>
        public float[] Waypoints = Array.Empty<float>();

        public float WalkSpeed = 5f;
        public float ArriveRange = 20f;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new SettingsValidationException("An NPC definition needs an Id.");
            string label = "NPC '" + Id + "'";

            if (string.IsNullOrWhiteSpace(SheepId))
                throw new SettingsValidationException(label + " needs a SheepId (the console's sheep_id).");
            if (string.IsNullOrWhiteSpace(RoomId))
                throw new SettingsValidationException(label + " needs a RoomId.");
            if (string.IsNullOrWhiteSpace(Description))
                throw new SettingsValidationException(label + " needs a Description.");
            if (Waypoints == null || Waypoints.Length == 0)
                throw new SettingsValidationException(label + " needs at least one waypoint.");
            foreach (float waypoint in Waypoints)
                if (float.IsNaN(waypoint) || float.IsInfinity(waypoint))
                    throw new SettingsValidationException(label + " has a waypoint that is not a finite number: " + waypoint);
            if (float.IsNaN(WalkSpeed) || WalkSpeed <= 0f)
                throw new SettingsValidationException(label + "'s WalkSpeed must be positive: " + WalkSpeed);
            if (float.IsNaN(ArriveRange) || ArriveRange <= 0f)
                throw new SettingsValidationException(label + "'s ArriveRange must be positive: " + ArriveRange);
        }

        /// <summary>U.INC-T25: parsed with Unity.Shared.Core's engine-free <see cref="JsonObject"/> reader.</summary>
        public static NpcDefinition FromJson(JsonObject json)
        {
            var npc = new NpcDefinition();
            if (json == null) return npc;
            npc.Id = json.GetString(nameof(Id), npc.Id);
            npc.SheepId = json.GetString(nameof(SheepId), npc.SheepId);
            npc.RoomId = json.GetString(nameof(RoomId), npc.RoomId);
            npc.Description = GmText.Expand(json.GetString(nameof(Description), npc.Description));
            npc.SpawnX = json.GetFloat(nameof(SpawnX), npc.SpawnX);
            npc.Waypoints = json.GetFloatArray(nameof(Waypoints));
            npc.WalkSpeed = json.GetFloat(nameof(WalkSpeed), npc.WalkSpeed);
            npc.ArriveRange = json.GetFloat(nameof(ArriveRange), npc.ArriveRange);
            return npc;
        }
    }
}
