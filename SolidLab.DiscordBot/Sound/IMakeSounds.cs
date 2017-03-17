using System.Threading.Tasks;
using Discord;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public interface IMakeSounds
    {
        Task Play(Channel channel, User user, object sound, SoundRequestType type, bool returnToChannel = false);
    }
}