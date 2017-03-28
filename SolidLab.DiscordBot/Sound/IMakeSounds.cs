using System.Threading.Tasks;
using Discord;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public interface IMakeSounds
    {
        Task Play(Channel channel, User user, AudioItem audio);
        Task Pause(Channel channel);
        Task Resume(Channel channel);
        Task Join(Channel channel);
        Task Disconnect();
        Channel GetCurrentChannel();
        int GetCurrentVolume();
        void SetVolume(int newVolume);
    }
}