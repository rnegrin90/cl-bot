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
    public class SingleSoundsService : IUseCommands
    {
        private ISoundsRepository _soundsRepo;
        private readonly AudioService _audioService;
        private readonly WaveFormat _waveFormat;
        private readonly int _blockSize;
        private readonly Queue<CommandQueueElement> _commandQueue;

        public SingleSoundsService(AudioService audioService)
        {
            _audioService = audioService;
            
            _waveFormat = new WaveFormat(48000, 16, _audioService.Config.Channels);
            _blockSize = _waveFormat.AverageBytesPerSecond/50;

            _commandQueue = new Queue<CommandQueueElement>();
        }

        public void SetUpCommands (CommandService cmdService)
        {
            cmdService.CreateCommand("sound")
                .Parameter("SoundName")
                .Alias("sd")
                .Description("Play a sound (If found!)")
                //.Do(e => Console.WriteLine("Playing sound"));
                .Do(async e => await PlaySound(e, e.GetArg("SoundName")));

            cmdService.CreateGroup("sound", s =>
            {
                s.CreateCommand("save")
                    .Parameter("Sound", ParameterType.Multiple)
                    //.AddCheck() TODO add check function
                    .Description("Store a sound with its command for it to be used later on!. Usage: ~sd save {sound} {command} [alias]")
                    .Do(e => Console.WriteLine("Adding sound"));
            });
        }

        private async Task PlaySound(CommandEventArgs ev, string soundName)
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
