using System;
using System.Linq;
using Discord.Audio;
using Discord.Commands;

namespace SolidLab.DiscordBot.Sound
{
    public class TestSummonService : IUseCommands
    {
        private readonly AudioService _audioService;

        public TestSummonService(AudioService audioService)
        {
            _audioService = audioService;
        }

        public void SetUpCommands(CommandService cmdService)
        {
            cmdService.CreateCommand("summon")
                .Alias("sm")
                .Description("The bot will join the voice channel you currently are.")
                .Do(e => Console.WriteLine("Playing sound"));

            cmdService.CreateGroup("summon", c =>
            {
                c.CreateCommand("all")
                    .Description("The bot will join all the existing voice channels")
                    .Do(async e =>
                    {
                        Console.WriteLine($"Channel count: {e.Server.VoiceChannels.Count()}");
                        foreach (var voiceChannel in e.Server.VoiceChannels)
                        {
                            Console.WriteLine(voiceChannel.Name);
                            await _audioService.Join(voiceChannel).ConfigureAwait(false);
                        }
                    });
            });
        }
    }
}
