using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public class MusicPlaylistService : IMakeSounds
    {
        public void Play(Channel channel, User user, object sound, SoundRequestType type)
        {
            throw new NotImplementedException();
        }

        public void Pause(CommandEventArgs ev)
        {
            ev.Channel.SendMessage("Music paused...");
        }
    }
}
