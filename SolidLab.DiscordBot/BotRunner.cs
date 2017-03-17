using System;
using System.Configuration;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using SolidLab.DiscordBot.Events;
using SolidLab.DiscordBot.Sound;
using SolidLab.DiscordBot.Functions;

namespace SolidLab.DiscordBot
{
    public class BotRunner : IRunner
    {
        private readonly DiscordClient _client;
        private int _returnValue;

        public BotRunner(DiscordClient client)
        {
            _client = client;
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

                _client.UsingAudio(x => // Opens an AudioConfigBuilder so we can configure our AudioService
                {
                    x.Mode = AudioMode.Outgoing; // Tells the AudioService that we will only be sending audio
                });

                // TODO DI!!!
                var simpleSoundService = new SimpleSoundService(_client.GetService<AudioService>());
                var playlistService = new MusicPlaylistService();
                var eventService = new EventService(new SoundsRepository(), simpleSoundService);
                var soundHandler = new SoundHandler(simpleSoundService, playlistService);
                var chatHandler = new ChatHandler();
                var cmdService = _client.GetService<CommandService>();
                soundHandler.SetUpCommands(cmdService);
                chatHandler.SetUpCommands(cmdService);
                cmdService.CreateCommand("restart")
                    .Do(e =>
                    {
                        _client.Disconnect();
                        _returnValue = 1;
                    });

                _client.UserUpdated += eventService.EventGenerated;
                
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
