using System;
using System.Configuration;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using SolidLab.DiscordBot.Events;

namespace SolidLab.DiscordBot
{
    public class BotRunner : IRunner
    {
        private readonly DiscordClient _client;
        private readonly IUseCommands _soundHandler;
        private readonly IUseCommands _chatHandler;
        private readonly IHandleEvents _userEventHandler;
        private readonly IHandleEvents _chatEventHandler;
        private int _returnValue;

        public BotRunner(
            DiscordClient client, 
            IUseCommands soundHandler, 
            IUseCommands chatHandler, 
            IHandleEvents userEventHandler, 
            IHandleEvents chatEventHandler)
        {
            _client = client;
            _soundHandler = soundHandler;
            _chatHandler = chatHandler;
            _userEventHandler = userEventHandler;
            _chatEventHandler = chatEventHandler;
            _returnValue = 0;
        }

        public async Task Run()
        {
            Console.WriteLine("Loading");
            try
            {
                _client.UsingCommands(c =>
                {
                    c.PrefixChar = ConfigurationManager.AppSettings["DiscordBot:Prefix"].ToCharArray()[0];
                    c.HelpMode = HelpMode.Public;
                });
                
                var cmdService = _client.GetService<CommandService>();
                Console.WriteLine("Creating commands...");
                _soundHandler.SetUpCommands(cmdService);
                Console.WriteLine("Setting up audio commands");
                _chatHandler.SetUpCommands(cmdService);
                Console.WriteLine("Setting up chat commands");
                cmdService.CreateCommand("restart")
                    .Do(e =>
                    {
                        _client.Disconnect();
                        _returnValue = 1;
                    });

                Console.WriteLine("Wiring events");
                _client.UserUpdated += _userEventHandler.EventGenerated;
                _client.MessageReceived += _chatEventHandler.EventGenerated;
                
                await _client
                    .Connect(ConfigurationManager.AppSettings["DiscordBot:Token"], TokenType.Bot)
                    .ConfigureAwait(false);

                Console.WriteLine("Client started");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task Stop()
        {
            await _client.Disconnect().ConfigureAwait(false);
        }
    }
}
