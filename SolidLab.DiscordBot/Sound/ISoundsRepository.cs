using System.Collections.Generic;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public interface ISoundsRepository
    {
        List<string> GetAvailableSounds();
        UserGreeting GetPersonalisedUserGreeting(ulong userId);
    }
}