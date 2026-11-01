using System.Threading.Tasks;
using Incognito.World;
using Unity.Multiplayer; // IBotDriver, NetBot, [NetBotDriver]
using UnityEngine;
using UnityEngine.Scripting;

namespace Incognito.Online
{
    /// <summary>
    /// Incognito's bot (U.INC-T43): launched with <c>-bot A</c> (mp-host game smoke, NetBotTest) the kit
    /// joins the game by code with no menus and hands the session to <see cref="RunAsync"/>. It plays like a
    /// player would, through the same [ToServer] calls: it walks into the room until the server has moved
    /// it, stops, then draws the gun until the server reports it drawn, and passes.
    /// </summary>
    [NetBotDriver, Preserve]
    public sealed class IncognitoBot : IBotDriver
    {
        /// <summary>Original px the server must have walked the bot before it counts the walk.</summary>
        public const float WalkPx = 100f;

        public async Task RunAsync(NetBot bot)
        {
            IncognitoNetPlayer me = await bot.LocalPlayer<IncognitoNetPlayer>();
            me.BotDriven = true; // stop Update() from reading real input over this driver's own calls (U.INC-T48)
            await bot.Until(() => me.Seat >= 0, "the server to seat this bot");
            float start = me.X;

            // Walk toward the room's middle, as OnlineTuning spaces the seats.
            float roomWidth = IncognitoOnlineGame.RoomWidth;
            int key = start > roomWidth * 0.5f ? OnlineControls.MoveLeftBit : OnlineControls.MoveRightBit;
            me.SendControls(key, 0f, 1920f);
            await bot.Until(() => Mathf.Abs(me.X - start) >= WalkPx, "the server to walk the bot " + WalkPx + " px", 20f);
            me.SendControls(0, 0f, 1920f);

            me.ToggleGunKey();
            await bot.Until(() => me.GunOut, "the server to draw the bot's gun", 20f);
            bot.Pass("seat " + me.Seat + " (" + me.PlayerId + ") walked from " + start.ToString("0") + " to "
                + me.X.ToString("0") + " px and drew the gun, as " + me.DisplayName);
        }
    }
}
