using System.Threading.Tasks;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public interface IDownloadAudio
    {
        Task<AudioItem> GetAudioStream(string link);
    }
}
