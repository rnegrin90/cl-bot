using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace SolidLab.DiscordBot.Sound
{
    public class MusicPlaylistService : IMakeSounds
    {
        public Task Play(CommandEventArgs ev, string soundName)
        {
            return null;
        }

        public void Pause(CommandEventArgs ev)
        {
            ev.Channel.SendMessage("Music paused...");
        }
    }
}
