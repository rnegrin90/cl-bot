﻿using System;
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
        private readonly IYoutubeDownloader _youtubeDownloader;
        private readonly ISoundsRepository _soundsRepo;
        private IAudioClient _audioClient;
        private readonly AudioService _audioService;
        private readonly PlayerSettings _settings;
        private readonly PlayerStatus _playerStatus;
        private readonly Queue<CommandQueueElement> _commandQueue;
        
        public SoundService(DiscordClient discordClient, IYoutubeDownloader youtubeDownloader)
        {
            _youtubeDownloader = youtubeDownloader;
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
                        audioStream = (await _youtubeDownloader
                                .GetAudioStream((string)sound)
                                .ConfigureAwait(false))
                            .FileStream;
                        break;
                }

                var byteBuffer = DiscordEncode(audioStream);
                await SendEncoded(byteBuffer, _playerStatus);
                
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

        private async Task<Stream> ProcessMp3File(string path)
        {
            return await Task.Run(() => File.OpenRead(path));
        }

        private IList<byte[]> DiscordEncode(Stream inputStream)
        {
            using (var mp3Reader = new Mp3FileReader(inputStream))
            {
                using (var resampler = new MediaFoundationResampler(mp3Reader, _settings.WaveFormat))
                {
                    resampler.ResamplerQuality = 60;
                    var buffer = new byte[_settings.BlockSize];
                    int byteCount;

                    var soundChunks = new List<byte[]>();
                    while ((byteCount = resampler.Read(buffer, 0, _settings.BlockSize)) > 0)
                    {
                        if (byteCount < _settings.BlockSize)
                        {
                            for (var i = byteCount; i < _settings.BlockSize; i++)
                                buffer[i] = 0;
                        }
                        soundChunks.Add(buffer);
                        buffer = new byte[_settings.BlockSize];
                    }

                    return soundChunks;
                }
            }
        }

        private async Task SendEncoded(IEnumerable<byte[]> buffer, PlayerStatus status)
        {
            try
            {
                // TODO disconnected channel throw
                foreach (var chunk in buffer.ToList())
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