using System;
using System.Configuration;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using SolidLab.DiscordBot.Sound;

namespace SolidLab.DiscordBot
{
    public class BotRunner : IRunner
    {
        private readonly DiscordClient _client;
        private SingleSoundsService _singleSoundsService;

        public BotRunner(DiscordClient client)
        {
            _client = client;
        }

        public Task Run()
        {
            Console.WriteLine("Running");

            _client.UsingCommands(c =>
            {
                c.PrefixChar = ConfigurationManager.AppSettings["DiscordBot:Prefix"].ToCharArray()[0];
                c.HelpMode = HelpMode.Public;
            });

            _client.UsingAudio(x => // Opens an AudioConfigBuilder so we can configure our AudioService
            {
                x.Mode = AudioMode.Outgoing; // Tells the AudioService that we will only be sending audio
            });


            _singleSoundsService = new SingleSoundsService(_client.GetService<AudioService>());
            var cmdService = _client.GetService<CommandService>();

            _singleSoundsService.SetUpCommands(cmdService);

            return _client.Connect(ConfigurationManager.AppSettings["DiscordBot:Token"], TokenType.Bot);
        }
    }
}
