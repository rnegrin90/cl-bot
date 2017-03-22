using System;
using System.Configuration;
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
        private int _returnValue;

        public BotRunner(
            DiscordClient client, 
            IUseCommands soundHandler, 
            IUseCommands chatHandler, 
            IHandleEvents userEventHandler)
        {
            _client = client;
            _soundHandler = soundHandler;
            _chatHandler = chatHandler;
            _userEventHandler = userEventHandler;
            _returnValue = 0;
        }

        public async void Run()
        {
            Console.WriteLine("Running");
            try
            {
                _client.UsingCommands(c =>
                {
                    c.PrefixChar = ConfigurationManager.AppSettings["DiscordBot:Prefix"].ToCharArray()[0];
                    c.HelpMode = HelpMode.Public;
                });
                
                var cmdService = _client.GetService<CommandService>();
                _soundHandler.SetUpCommands(cmdService);
                _chatHandler.SetUpCommands(cmdService);
                cmdService.CreateCommand("restart")
                    .Do(e =>
                    {
                        _client.Disconnect();
                        _returnValue = 1;
                    });

                _client.UserUpdated += _userEventHandler.EventGenerated;
                
                await _client
                    .Connect(ConfigurationManager.AppSettings["DiscordBot:Token"], TokenType.Bot)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
