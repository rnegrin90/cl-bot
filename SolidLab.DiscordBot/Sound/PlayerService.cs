using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using NAudio.Wave;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public class PlayerService : IMakeSounds
    {
        private readonly DiscordClient _discordClient;
        private IAudioClient _audioClient;
        private AudioService _audioService;
        private readonly PlayerSettings _settings;
        private readonly PlayerStatus _playerStatus;
        private readonly Queue<CommandQueueElement> _commandQueue;
        
        public PlayerService(DiscordClient discordClient)
        {
            _discordClient = discordClient;
            _playerStatus = new PlayerStatus
            {
                Status = InternalStatus.Starting,
                StatusMessage = string.Empty
            };

            discordClient.UsingAudio(a => a.Mode = AudioMode.Outgoing);
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
                await Task.Run(async () =>
                {
                    while (_audioClient != null)
                    {
                        _audioClient.VoiceSocket.SendHeartbeat();
                        await Task.Delay(100);
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _audioService = _discordClient.GetService<AudioService>();
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

        public async Task Play(Channel channel, User user, AudioItem audio)
        {
            try
            {
                audio.FileStream.Position = 0;
                _playerStatus.PlayingItem = audio;

                await StreamToDiscord(audio.FileStream).ConfigureAwait(false);

                _playerStatus.Status = InternalStatus.Idle;
            }
            catch (Exception e)
            {
                _playerStatus.Status = InternalStatus.Idle;
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task Pause(Channel channel)
        {
            Console.WriteLine("Pausing player");
            if (_playerStatus.Status != InternalStatus.Playing)
            {
                await channel.SendMessage("There is nothing playing right now.").ConfigureAwait(false);
            }
            _playerStatus.Status = InternalStatus.Paused;
        }

        public async Task Resume(Channel channel)
        {
            Console.WriteLine("Resuming player");
            if (_playerStatus.Status != InternalStatus.Paused)
            {
                await channel.SendMessage("Can't resume, player is not Paused").ConfigureAwait(false);
            }
            _playerStatus.Status = InternalStatus.Playing;
        }

        private async Task StreamToDiscord(Stream inputStream)
        {
            using (var mp3Reader = new Mp3FileReader(inputStream))
            {
                _playerStatus.PlayingItem.Duration = mp3Reader.TotalTime;
                var waveChannel32 = new WaveChannel32(mp3Reader);
                using (var resampler = new MediaFoundationResampler(waveChannel32, _settings.WaveFormat))
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
                    while ((byteCount = resampler.Read(buffer, 0, _settings.BlockSize)) > 0 && waveChannel32.Position <= waveChannel32.Length)
                    {
                        waveChannel32.Volume = _settings.Volume;

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

        public int GetCurrentVolume()
        {
            return (int) (_settings.Volume * 100);
        }

        public void SetVolume(int newVolume)
        {
            if (newVolume >= 0 && newVolume <= 100)
            {
                _settings.Volume = newVolume / 100f;
            }
            else
            {
                throw new Exception("Invalid value");
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
