using System.Threading.Tasks;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public interface IYoutubeDownloader
    {
        Task<YoutubeData> GetAudioStream(string link);
    }
}
