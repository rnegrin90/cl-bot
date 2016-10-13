using System;
using System.Configuration;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace SolidLab.DiscordBot
{
    public class BotRunner : IRunner
    {
        private readonly DiscordClient _client;

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

            return _client.Connect(ConfigurationManager.AppSettings["DiscordBot:Token"], TokenType.Bot);
        }
    }
}
