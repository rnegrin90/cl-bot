using System.Collections.Generic;

namespace SolidLab.DiscordBot.Sound
{
    public interface ISoundsRepository
    {
        List<string> GetAvailableSounds();
    }
}