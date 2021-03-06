﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public class SoundCommandsHandler : IUseCommands
    {
        private readonly IMakeSounds _soundService;
        private readonly ISoundsRepository _soundsRepository;
        private readonly ISearchSounds _searchService;

        public SoundCommandsHandler(IMakeSounds soundService, ISoundsRepository soundsRepository, ISearchSounds searchService)
        {
            _soundService = soundService;
            _soundsRepository = soundsRepository;
            _searchService = searchService;
        }

        public void SetUpCommands(CommandService cmdService)
        {
            cmdService.CreateCommand("play")
                .Parameter("SoundName", ParameterType.Multiple)
                .Alias("sd")
                .Description("Play a sound (If found!)")
                .Do(ProcessPlayEvent);

            cmdService.CreateCommand("pause")
                .Description("Pauses music")
                .Do(async e => await _soundService.Pause(e.Channel).ConfigureAwait(false));
            
            cmdService.CreateCommand("resume")
                .Description("Resumes music")
                .Do(async e => await _soundService.Resume(e.Channel).ConfigureAwait(false));

            cmdService.CreateCommand("volume")
                .Parameter("Volume", ParameterType.Optional)
                .Description("Sets the bot volume")
                .Do(async e => await ProcessVolume(e).ConfigureAwait(false));
            
            cmdService.CreateCommand("join")
                .Parameter("ChannelName", ParameterType.Multiple)
                .Description("Joins user current voice channel")
                .Do(async e =>
                {
                    Console.WriteLine("Joining a voice channel");
                    if (e.Args.Length > 0)
                    {
                        var channelName = string.Join(" ", e.Args);
                        var channel = e.Server.VoiceChannels.FirstOrDefault(c => c.Name == channelName);
                        if (channel != null)
                        {
                            await _soundService.Join(channel).ConfigureAwait(false);
                        }
                        else
                        {
                            await e.Channel.SendMessage("Channel not found").ConfigureAwait(false);
                            return;
                        }
                    }
                    if (e.User.VoiceChannel == null)
                        await e.Channel.SendMessage("You need to specify a channel for me to join!").ConfigureAwait(false);
                    await _soundService.Join(e.User.VoiceChannel).ConfigureAwait(false);
                });

            cmdService.CreateCommand("disconnect")
                .Description("Leaves the current voice channel")
                .Do(async e =>
                {
                    await _soundService.Disconnect().ConfigureAwait(false);
                });

            cmdService.CreateGroup("set", s =>
            {
                s.CreateCommand("greet")
                    .Parameter("Sound", ParameterType.Multiple)
                    //.Description("Store a sound with its command for it to be used later on!. Usage: ~sd save {sound} {command} [alias]")
                    .Description("Assign a sound to greet the user when it joins the channel")
                    .Do(async e =>
                    {
                        Console.WriteLine($"Setting custom greeting for user {e.User.Id}");

                        var param = string.Join(" ", e.Args);
                        var audio = await _soundsRepository.GetAudioItem(param, DetectSoundType(param), e.User.Id).ConfigureAwait(false);

                        Console.WriteLine($"{audio.SongTitle} set as custom greeting");

                        audio.SongTitle += "-" + e.User.Id;
                        await _soundsRepository.StoreSound(audio, SoundUse.Greeting).ConfigureAwait(false);
                    });
            });
        }

        private async Task ProcessVolume(CommandEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                if (int.TryParse(e.GetArg("Volume"), out int newVol))
                {
                    try
                    {
                        _soundService.SetVolume(newVol);
                    }
                    catch (Exception ex)
                    {
                        await e.Channel.SendMessage(ex.Message).ConfigureAwait(false);
                    }
                }
                else
                {
                    await e.Channel.SendMessage("Value must be a valid number").ConfigureAwait(false);
                }
            }
            else
            {
                await e.Channel.SendMessage($"The current volume is {_soundService.GetCurrentVolume()}").ConfigureAwait(false);
            }
        }

        public async Task ProcessPlayEvent(CommandEventArgs ev)
        {
            if (ev.Args.Length == 0)
            {
                await ev.Channel.SendMessage("You need to give me something to play!");
                return;
            }

            Console.WriteLine($"Starting to play: `{string.Join(" ", ev.Args)}`");
            if (ev.User.VoiceChannel.Id != _soundService.GetCurrentChannel()?.Id)
                await _soundService.Join(ev.User.VoiceChannel).ConfigureAwait(false);
                //await ev.Channel.SendMessage("You must be in a voice channel to use this command!").ConfigureAwait(false);

            var soundName = ev.GetArg("SoundName");

            if (ev.Args.Length > 1 || !IsUrl(soundName))
            {
                soundName = await _searchService.Search(string.Join(" ", ev.Args));
            }
            
            var audioItem = await _soundsRepository.GetAudioItem(soundName, DetectSoundType(soundName), ev.User.Id).ConfigureAwait(false);

            Console.WriteLine($"Storing sound: `{audioItem.Link}`");
            await _soundsRepository.StoreSound(audioItem, SoundUse.StoredSound).ConfigureAwait(false);

            Console.WriteLine($"Playing: `{audioItem.Link}`");
            await _soundService.Play(ev.User.VoiceChannel, ev.User, audioItem).ConfigureAwait(false);
        }

        private bool IsUrl(string soundName)
        {
            return Uri.TryCreate(soundName, UriKind.Absolute, out Uri uriResult) 
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private SoundRequestType DetectSoundType(string soundName)
        {
            Uri uriResult;
            if (Uri.TryCreate(soundName, UriKind.Absolute, out uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                var youtubeDomains = new[] {"www.youtube.com", "m.youtube.com", "youtu.be", "m.youtu.be"};
                if (youtubeDomains.Any(d => d.Contains(uriResult.Host)))
                    return SoundRequestType.Youtube;

                var soundcloudDomains = new[] {"www.soundcloud.com", "soundcloud.com", "m.soundcloud.com"};
                if (soundcloudDomains.Any(d => d.Contains(uriResult.Host)))
                    return SoundRequestType.Soundcloud;

                var fileExtensionsAccepted = new[] {".mp3"};
                if (fileExtensionsAccepted.Any(e => uriResult.PathAndQuery.Contains(e)))
                    return SoundRequestType.LinkMp3;
            }
            return SoundRequestType.SearchString;
        }
    }
}
