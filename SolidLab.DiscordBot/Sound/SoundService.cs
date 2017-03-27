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

namespace SolidLab.DiscordBot.Sound
{
    public class SoundService : IMakeSounds
    {
        private readonly IDownloadAudio _youtubeDownloader;
        private readonly IDownloadAudio _mp3Downloader;
        private readonly ISoundsRepository _soundsRepo;
        private IAudioClient _audioClient;
        private readonly AudioService _audioService;
        private readonly PlayerSettings _settings;
        private readonly PlayerStatus _playerStatus;
        private readonly Queue<CommandQueueElement> _commandQueue;
        
        public SoundService(
            DiscordClient discordClient, 
            IDownloadAudio youtubeDownloader,
            IDownloadAudio mp3Downloader)
        {
            _youtubeDownloader = youtubeDownloader;
            _mp3Downloader = mp3Downloader;
            _playerStatus = new PlayerStatus
            {
                Status = InternalStatus.Starting,
                StatusMessage = string.Empty
            };
            _audioService = discordClient.GetService<AudioService>();

            _settings = new PlayerSettings(new WaveFormat(48000, 16, _audioService.Config.Channels));
            
            _commandQueue = new Queue<CommandQueueElement>();
            _playerStatus.Status = InternalStatus.Idle;
        }

        public async Task Join(Channel channel)
        {
            try
            {
                _audioClient = await _audioService.Join(channel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task Disconnect()
        {
            if (_audioClient != null)
            {
                await _audioService.Leave(_audioClient.Channel).ConfigureAwait(false);
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
                IDownloadAudio selectedDownloader = null;
                switch (type)
                {
                    case SoundRequestType.LinkMp3:
                        selectedDownloader = _mp3Downloader;
                        break;
                    case SoundRequestType.Youtube:
                        selectedDownloader = _youtubeDownloader;
                        break;
                }

                if (selectedDownloader != null)
                {
                    var audioData = await selectedDownloader.GetAudioStream((string)sound).ConfigureAwait(false);

                    audioData.CreatorId = user.Id;
                    _playerStatus.PlayingItem = audioData;

                    await DiscordEncode(audioData.FileStream).ConfigureAwait(false); // TODO I think it will always be a string
                }

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
                await channel.SendMessage("There is nothing playing right now.").ConfigureAwait(false);
            }
            _playerStatus.Status = InternalStatus.Paused;
        }

        public async Task Resume(Channel channel)
        {
            if (_playerStatus.Status != InternalStatus.Paused)
            {
                await channel.SendMessage("Can't resume, player is not Paused").ConfigureAwait(false);
            }
            _playerStatus.Status = InternalStatus.Playing;
        }

        private async Task DiscordEncode(Stream inputStream)
        {
            using (var mp3Reader = new Mp3FileReader(inputStream))
            {
                _playerStatus.PlayingItem.Duration = mp3Reader.TotalTime;
                using (var resampler = new MediaFoundationResampler(mp3Reader, _settings.WaveFormat))
                {
                    resampler.ResamplerQuality = 60;
                    var buffer = new byte[_settings.BlockSize];
                    int byteCount;

                    while (_playerStatus.Status != InternalStatus.Idle)
                    {
                        Console.WriteLine("Waiting until current song finishes");
                        await Task.Delay(1);
                    }

                    _playerStatus.Status = InternalStatus.Playing;
                    while ((byteCount = resampler.Read(buffer, 0, _settings.BlockSize)) > 0)
                    {
                        if (byteCount < _settings.BlockSize)
                        {
                            for (var i = byteCount; i < _settings.BlockSize; i++)
                                buffer[i] = 0;
                        }

                        while (_playerStatus.Status == InternalStatus.Paused)
                        {
                            await Task.Delay(1).ConfigureAwait(false);
                            _audioClient.VoiceSocket.SendHeartbeat();
                        }

                        if (_playerStatus.Status == InternalStatus.Stopped)
                            return;

                        _audioClient.VoiceSocket.SendHeartbeat();

                        _audioClient.Send(buffer, 0, _settings.BlockSize);
                    }

                    _audioClient.Wait();
                }
            }
        }

        private async Task SendEncoded(IList<byte[]> buffer, PlayerStatus status)
        {
            while (_playerStatus.Status != InternalStatus.Idle)
            {
                Console.WriteLine("Waiting until current song finishes");
                await Task.Delay(1).ConfigureAwait(false);
            }
            try
            {
                _playerStatus.Status = InternalStatus.Playing;
                _playerStatus.ElapsedPercentage = 0;
                var chunkValue = 100d / buffer.Count;
                // TODO disconnected channel throw
                foreach (var chunk in buffer)
                {
                    while (status.Status == InternalStatus.Paused)
                    {
                        await Task.Delay(1);
                        _audioClient.VoiceSocket.SendHeartbeat();
                    }

                    if (status.Status == InternalStatus.Stopped)
                        return;

                    _audioClient.VoiceSocket.SendHeartbeat();

                    _audioClient.Send(chunk, 0, _settings.BlockSize);
                    _playerStatus.ElapsedPercentage += chunkValue;
                }
                _audioClient.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public class CommandQueueElement
    {
        public Func<CommandEventArgs, string, Task> Function { get; set; }
        public CommandEventArgs EventArgs { get; set; }
        public string CommandArgs { get; set; }
    }
}
