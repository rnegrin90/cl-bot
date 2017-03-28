using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaToolkit;
using MediaToolkit.Model;
using SolidLab.DiscordBot.Sound.Models;
using YoutubeExtractor;

namespace SolidLab.DiscordBot.Sound
{
    public class YoutubeDownloader : IDownloadAudio
    {
        private readonly SoundQuality _soundQuality;
        private readonly string _soundCache;

        public YoutubeDownloader(string soundQuality, string soundCache)
        {
            _soundQuality = GetSoundQuality(soundQuality);
            _soundCache = soundCache;
        }

        public async Task<AudioItem> GetAudioStream(string link)
        {
            var videoInfos = DownloadUrlResolver
                .GetDownloadUrls(link)
                .Where(i => i.VideoType == VideoType.Mp4)
                .Where(i => i.AudioBitrate != 0)
                .OrderByDescending(info => info.AudioBitrate);

            VideoInfo video;
            switch (_soundQuality)
            {
                case SoundQuality.Low:
                    video = videoInfos.Last();
                    break;
                case SoundQuality.Medium:
                    video = videoInfos.Skip(videoInfos.Count() / 2).First();
                    break;
                case SoundQuality.High:
                    video = videoInfos.First();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (video.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(video);
            }

            var fileName = video.Title;
            foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidFileNameChar.ToString(), "");
            }

            var downloader = new VideoDownloader(video, Path.Combine(_soundCache, fileName + video.VideoExtension));

            downloader.Execute();

            return new AudioItem
            {
                FileStream = await ConvertToMp3(_soundCache, fileName, video.VideoExtension),
                Link = link,
                Mp3Path = Path.Combine(_soundCache, fileName + ".mp3"),
                SongTitle = video.Title,
                Duration = GetDuration(Path.Combine(_soundCache, fileName + ".mp3"))
            };
        }

        private TimeSpan GetDuration(string filePath)
        {
            var file = new MediaFile { Filename = filePath };

            using (var engine = new Engine())
            {
                engine.GetMetadata(file);
            }

            return file.Metadata.Duration;
        }

        private async Task<FileStream> ConvertToMp3(string path, string videoName, string extension)
        {
            var inputFile = new MediaFile { Filename = Path.Combine(path, videoName + extension) };
            var outputFile = new MediaFile { Filename = Path.Combine(path, videoName + ".mp3") };

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
            }

            File.Delete(Path.Combine(path, videoName + extension));

            return await Task.Run(() => File.OpenRead(Path.Combine(path, videoName + ".mp3")));
        }

        private static SoundQuality GetSoundQuality(string soundQuality)
        {
            return Enum.TryParse(soundQuality, out SoundQuality returnValue) ? returnValue : SoundQuality.Low;
        }
    }
}