using System;
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

        public SoundCommandsHandler(IMakeSounds soundService, ISoundsRepository soundsRepository)
        {
            _soundService = soundService;
            _soundsRepository = soundsRepository;
        }

        public void SetUpCommands(CommandService cmdService)
        {
            cmdService.CreateCommand("play")
                .Parameter("SoundName", ParameterType.Multiple)
                .Alias("sd")
                .Description("Play a sound (If found!)")
                .Do(async e => await ProcessPlayEvent(e).ConfigureAwait(false));

            cmdService.CreateCommand("pause")
                .Description("Pauses music")
                .Do(async e => await _soundService.Pause(e.Channel).ConfigureAwait(false));
            
            cmdService.CreateCommand("resume")
                .Description("Resumes music")
                .Do(async e => await _soundService.Resume(e.Channel).ConfigureAwait(false));

            cmdService.CreateCommand("volume")
                .Parameter("Volume", ParameterType.Optional)
                .Description("Sets the bot volume")
                .Do(async e =>
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
                });
            
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

            cmdService.CreateGroup("sound", s =>
            {
                s.CreateCommand("save")
                    .Parameter("Sound", ParameterType.Multiple)
                    .Description("Store a sound with its command for it to be used later on!. Usage: ~sd save {sound} {command} [alias]")
                    .Do(e => Console.WriteLine("Adding sound"));
            });
        }

        public async Task ProcessPlayEvent(CommandEventArgs ev)
        {
            Console.WriteLine($"Playing sound `{string.Join(" ", ev.Args)}`");
            if (ev.User.VoiceChannel.Id != _soundService.GetCurrentChannel()?.Id)
                await _soundService.Join(ev.User.VoiceChannel).ConfigureAwait(false);
                //await ev.Channel.SendMessage("You must be in a voice channel to use this command!").ConfigureAwait(false);

            var soundName = ev.GetArg("SoundName");

            var audioItem = await _soundsRepository.GetAudioItem(soundName, DetectSoundType(soundName), ev.User.Id).ConfigureAwait(false);
            
            await _soundService.Play(ev.User.VoiceChannel, ev.User, audioItem).ConfigureAwait(false);
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
