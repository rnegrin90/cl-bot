using System;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Discord.Commands;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public class SoundHandler : IUseCommands
    {
        private readonly IMakeSounds _soundService;

        public SoundHandler(IMakeSounds soundService)
        {
            _soundService = soundService;
        }

        public void SetUpCommands(CommandService cmdService)
        {
            cmdService.CreateCommand("play")
                .Parameter("SoundName", ParameterType.Multiple)
                .Alias("sd")
                .Description("Play a sound (If found!)")
                .Do(async e => await ProcessPlayEvent(e));

            cmdService.CreateCommand("pause")
                .Description("Pauses music")
                .Do(e => _soundService.Pause(e.Channel));
            
            cmdService.CreateCommand("resume")
                .Description("Resumes music")
                .Do(e => _soundService.Resume(e.Channel));
            
            cmdService.CreateCommand("join")
                .Parameter("ChannelName", ParameterType.Multiple)
                .Description("Joins user current voice channel")
                .Do(async e =>
                {
                    if (e.Args.Length > 0)
                    {
                        var channelName = string.Join(" ", e.Args);
                        var channel = e.Server.VoiceChannels.FirstOrDefault(c => c.Name == channelName);
                        if (channel != null)
                        {
                            await _soundService.Join(channel);
                        }
                        else
                        {
                            await e.Channel.SendMessage("Channel not found");
                            return;
                        }
                    }
                    if (e.User.VoiceChannel == null)
                        await e.Channel.SendMessage("You need to specify a channel for me to join!");
                    await _soundService.Join(e.User.VoiceChannel);
                });

            cmdService.CreateCommand("disconnect")
                .Description("Leaves the current voice channel")
                .Do(async e =>
                {
                    await _soundService.Disconnect();
                });

            cmdService.CreateGroup("sound", s =>
            {
                s.CreateCommand("save")
                    .Parameter("Sound", ParameterType.Multiple)
                    //.AddCheck() TODO add check function
                    .Description("Store a sound with its command for it to be used later on!. Usage: ~sd save {sound} {command} [alias]")
                    .Do(e => Console.WriteLine("Adding sound"));
            });
        }

        public async Task ProcessPlayEvent(CommandEventArgs ev)
        {
            if (ev.User.VoiceChannel.Id != _soundService.GetCurrentChannel()?.Id)
                await ev.Channel.SendMessage("You must be in a voice channel to use this command!");

            var soundName = ev.GetArg("SoundName");
            
            await _soundService.Play(ev.User.VoiceChannel, ev.User, soundName, DetectSoundType(soundName));
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
