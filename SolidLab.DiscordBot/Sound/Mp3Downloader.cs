using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public class Mp3Downloader : IDownloadAudio
    {
        private readonly string _soundCache;

        public Mp3Downloader(string soundCache)
        {
            _soundCache = soundCache;
        }

        public async Task<AudioItem> GetAudioStream(string link)
        {
            var filename = GetSoundTitle(link);
            var fileLocation = await DownloadFile(link, filename);

            return new AudioItem
            {
                FileStream = File.OpenRead(fileLocation),
                SongTitle = filename,
                Link = link,
                Mp3Path = fileLocation,
                Mp4Path = null
            };
        }

        private async Task<string> DownloadFile(string link, string title)
        {
            var fileLocation = Path.Combine(_soundCache, title + ".mp3");

            if (!File.Exists(fileLocation))
            {
                using (var wc = new WebClient())
                {
                    await wc.DownloadFileTaskAsync(new Uri(link), fileLocation);
                }
            }

            return fileLocation;
        }

        private string GetSoundTitle(string link)
        {
            var regex = new Regex(".*[//=?&](.*).mp3.*");

            var match = regex.Match(link);
            
            if (match.Groups.Count > 1)
                return match.Groups[1].Value;
            else
                throw new Exception("Can't find file name");
        }
    }
}
