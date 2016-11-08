using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using NAudio.Wave;
using SolidLab.DiscordBot.Sound.Models;
using YoutubeExtractor;

namespace SolidLab.DiscordBot.Sound
{
    public class SimpleSoundService : IMakeSounds
    {
        private ISoundsRepository _soundsRepo;
        private readonly AudioService _audioService;
        private readonly WaveFormat _waveFormat;
        private readonly int _blockSize;
        private readonly Queue<CommandQueueElement> _commandQueue;

        public SimpleSoundService(AudioService audioService)
        {
            _audioService = audioService;
            
            _waveFormat = new WaveFormat(48000, 16, _audioService.Config.Channels);
            _blockSize = _waveFormat.AverageBytesPerSecond/50;

            _commandQueue = new Queue<CommandQueueElement>();
        }

        public async Task Play(CommandEventArgs ev, string soundName)
        {
            try
            {
                var userVoiceChannel = ev.User.VoiceChannel;

                if (userVoiceChannel == null)
                {
                    await ev.Channel.SendMessage("You must be in a voice channel to use this command!");
                    return;
                }

                var soundType = GetSoundType(soundName);

                Stream audioStream = null;
                switch (soundType)
                {
                    case SoundRequestType.Mp3File:
                        audioStream = ProcessMp3File(soundName);
                        break;
                    case SoundRequestType.Youtube:
                        audioStream = ProcessYoutube(soundName);
                        break;
                }

                var byteBuffer = DiscordEncode(audioStream);
                await SendEncoded(userVoiceChannel, byteBuffer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private Stream ProcessYoutube(string soundName)
        {
            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(soundName);

            VideoInfo video = videoInfos
                //.Where(info => info.CanExtractAudio)
                .OrderByDescending(info => info.AudioBitrate)
                .First();

            if (video.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(video);
            }

            var fileName = video.Title + video.AudioExtension;

            var audioDownloader = new AudioDownloader(video, Path.Combine("./DownloadCache", video.Title + video.AudioExtension));
            
            audioDownloader.Execute();

            return ProcessMp3File(fileName);
        }

        private Stream ProcessMp3File(string soundName)
        {
            return File.OpenRead($"./TempResources/{soundName}.mp3");
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

        private async Task SendEncoded(Channel userVoiceChannel, IEnumerable<byte[]> buffer)
        {
            try
            {
                var discordAudioClient = await _audioService.Join(userVoiceChannel);
                foreach (var chunk in buffer.ToList())
                {
                    discordAudioClient.Send(chunk, 0, _blockSize);
                }
                discordAudioClient.Wait();

                await _audioService.Leave(userVoiceChannel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private SoundRequestType GetSoundType(string soundName)
        {
            if (soundName.Contains("youtube"))
            {
                return SoundRequestType.Youtube;
            }
            return SoundRequestType.Mp3File;
        }
    }

    public class CommandQueueElement
    {
        public Func<CommandEventArgs, string, Task> Function { get; set; }
        public CommandEventArgs EventArgs { get; set; }
        public string CommandArgs { get; set; }
    }
}
