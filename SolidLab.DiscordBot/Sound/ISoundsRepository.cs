using System.Collections.Generic;
using System.Threading.Tasks;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public interface ISoundsRepository
    {
        List<string> GetAvailableSounds();
        UserGreeting GetPersonalisedUserGreeting(ulong userId);
        Task<AudioItem> GetAudioItem(string sound, SoundRequestType soundType, ulong userId);
    }
}