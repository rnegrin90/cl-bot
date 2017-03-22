using System.IO;

namespace SolidLab.DiscordBot.Sound.Models
{
    public class YoutubeData
    {
        public string Link { get; set; }
        public string SongTitle { get; set; }
        public Stream FileStream { get; set; }
        public string Mp3Path { get; set; }
        public string Mp4Path { get; set; }
    }
}