using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using MediaToolkit;
using MediaToolkit.Model;
using NAudio.Wave;
using SolidLab.DiscordBot.Sound.Models;
using YoutubeExtractor;

namespace SolidLab.DiscordBot.Sound
{
    public class SoundService : IMakeSounds
    {
        private readonly ISoundsRepository _soundsRepo;
        private readonly AudioService _audioService;
        private IAudioClient _audioClient;
        private readonly WaveFormat _waveFormat;
        private readonly int _blockSize;
        private readonly string _cacheFolderPath;
        private readonly SoundQuality _soundQuality;
        private readonly PlayerStatus _playerStatus;
        private readonly Queue<CommandQueueElement> _commandQueue;

        public SoundService(DiscordClient discordClient, string cacheFolderPath, string soundQuality)
        {
            _playerStatus = new PlayerStatus
            {
                Status = InternalStatus.Starting,
                StatusMessage = string.Empty
            };
            _audioService = discordClient.GetService<AudioService>();
            _cacheFolderPath = cacheFolderPath;

            _waveFormat = new WaveFormat(48000, 16, _audioService.Config.Channels);
            _blockSize = _waveFormat.AverageBytesPerSecond/50;
            _soundQuality = GetSoundQuality(soundQuality);

            _commandQueue = new Queue<CommandQueueElement>();
            _playerStatus.Status = InternalStatus.Idle;
        }

        public async Task Join(Channel channel)
        {
            _audioClient = await _audioService.Join(channel);
        }

        public async Task Disconnect()
        {
            if (_audioClient != null)
            {
                await _audioService.Leave(_audioClient.Channel);
                _audioClient = null;
            }
        }

        public Channel GetCurrentChannel()
        {
            return _audioClient?.Channel;
        }

        public async Task Play(Channel channel, User user, object sound, SoundRequestType type)
        {
            try
            {
                _playerStatus.Status = InternalStatus.Playing;

                Stream audioStream = null;
                switch (type)
                {
                    case SoundRequestType.Mp3File:
                        audioStream = await ProcessMp3File((string) sound);
                        break;
                    case SoundRequestType.Youtube:
                        audioStream = await ProcessYoutube((string) sound);
                        break;
                }

                var byteBuffer = DiscordEncode(audioStream);
                await SendEncoded(channel, byteBuffer, _playerStatus);
                
                _playerStatus.Status = InternalStatus.Idle;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task Pause(Channel channel)
        {
            if (_playerStatus.Status != InternalStatus.Playing)
            {
                await channel.SendMessage("There is nothing playing right now.");
            }
            _playerStatus.Status = InternalStatus.Paused;
        }

        public async Task Resume(Channel channel)
        {
            if (_playerStatus.Status != InternalStatus.Paused)
            {
                await channel.SendMessage("Can't resume, player is not Paused");
            }
            _playerStatus.Status = InternalStatus.Playing;
        }

        private async Task<Stream> ProcessYoutube(string soundName)
        {
            var videoInfos = DownloadUrlResolver
                .GetDownloadUrls(soundName)
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

            var downloader = new VideoDownloader(video, Path.Combine(_cacheFolderPath, video.Title + video.VideoExtension));

            downloader.Execute();

            return await ConvertToMp3(_cacheFolderPath, video.Title, video.VideoExtension);
        }

        private async Task<Stream> ConvertToMp3(string path, string videoName, string extension)
        {
            var inputFile = new MediaFile { Filename = Path.Combine(path, videoName + extension) };
            var outputFile = new MediaFile { Filename = Path.Combine(path, videoName + ".mp3") };

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
            }

            File.Delete(Path.Combine(path, videoName + extension));

            return await ProcessMp3File(Path.Combine(path, videoName + ".mp3"));
        }

        private async Task<Stream> ProcessMp3File(string path)
        {
            return await Task.Run(() => File.OpenRead(path));
        }

        private IList<byte[]> DiscordEncode(Stream inputStream)
        {
            using (var mp3Reader = new Mp3FileReader(inputStream))
            {
                using (var resampler = new MediaFoundationResampler(mp3Reader, _waveFormat))
                {
                    resampler.ResamplerQuality = 60;
                    var buffer = new byte[_blockSize];
                    int byteCount;

                    var soundChunks = new List<byte[]>();
                    while ((byteCount = resampler.Read(buffer, 0, _blockSize)) > 0)
                    {
                        if (byteCount < _blockSize)
                        {
                            for (var i = byteCount; i < _blockSize; i++)
                                buffer[i] = 0;
                        }
                        soundChunks.Add(buffer);
                        buffer = new byte[_blockSize];
                    }

                    return soundChunks;
                }
            }
        }

        private async Task SendEncoded(Channel userVoiceChannel, IEnumerable<byte[]> buffer, PlayerStatus status)
        {
            try
            {
                var discordAudioClient = await _audioService.Join(userVoiceChannel);
                // TODO disconnected channel throw
                foreach (var chunk in buffer.ToList())
                {
                    while (status.Status == InternalStatus.Paused)
                    {
                        await Task.Delay(1);
                    }

                    if (status.Status == InternalStatus.Stopped)
                        return;

                    _audioClient.Send(chunk, 0, _blockSize);
                }
                _audioClient.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static SoundQuality GetSoundQuality(string soundQuality)
        {
            return Enum.TryParse(soundQuality, out SoundQuality returnValue) ? returnValue : SoundQuality.Low;
        }
    }

    public class CommandQueueElement
    {
        public Func<CommandEventArgs, string, Task> Function { get; set; }
        public CommandEventArgs EventArgs { get; set; }
        public string CommandArgs { get; set; }
    }
}
