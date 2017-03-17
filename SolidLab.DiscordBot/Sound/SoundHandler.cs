using System;
using Discord.Commands;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    public class SoundHandler : IUseCommands
    {
        private MusicBotMode _botMode;
        private IMakeSounds _activeSoundService;
        private readonly SimpleSoundService _simpleSoundService;
        private readonly MusicPlaylistService _playlistService;

        public SoundHandler(SimpleSoundService simpleSoundService, MusicPlaylistService playlistService)
        {
            _simpleSoundService = simpleSoundService;
            _playlistService = playlistService;

            _botMode = MusicBotMode.Meme;
            _activeSoundService = _simpleSoundService;
        }

        public void SetUpCommands(CommandService cmdService)
        {
            cmdService.CreateCommand("audiomode")
                .Description("Changes the mode the bot plays music/sounds")
                .Do(e => e.Channel.SendMessage(PrintBotMode()));

            cmdService.CreateGroup("audiomode", c =>
            {
                c.CreateCommand("meme")
                    .Description("The bot will play meme sounds (max 15sec), will move between channels to play sounds, will be idle by default, will not notify of what is being played")
                    .Do(e =>
                    {
                        SetBotMode(MusicBotMode.Meme);
                        e.Channel.SendMessage("Meme mode activated");
                    });

                c.CreateCommand("music")
                    .Description("The bot will play music (10 min max), it will not move to other channel unless explitly told to do so, will play default music, will notify of what is being played.")
                    .Do(e =>
                    {
                        SetBotMode(MusicBotMode.Music);
                        e.Channel.SendMessage("Music mode activated");
                    });
            });

            cmdService.CreateCommand("play")
                .Parameter("SoundName")
                .Alias("sd")
                .Description("Play a sound (If found!)")
                .Do(e =>
                {
                    var soundName = e.GetArg("SoundName");
                    _activeSoundService.Play(e.User.VoiceChannel, e.User, soundName, DetectSoundType(soundName));
                });

            cmdService.CreateCommand("pause")
                .Description("Pauses music")
                .AddCheck((c, u, a) => _botMode == MusicBotMode.Music)
                .Do(e => _playlistService.Pause(e));

            cmdService.CreateGroup("sound", s =>
            {
                s.CreateCommand("save")
                    .Parameter("Sound", ParameterType.Multiple)
                    //.AddCheck() TODO add check function
                    .Description("Store a sound with its command for it to be used later on!. Usage: ~sd save {sound} {command} [alias]")
                    .Do(e => Console.WriteLine("Adding sound"));
            });
        }

        public string PrintBotMode()
        {
            return $"The bot is in {_botMode}";
        }

        public void SetBotMode(MusicBotMode mode)
        {
            switch (mode)
            {
                case MusicBotMode.Meme:
                    _botMode = mode;
                    _activeSoundService = _simpleSoundService;
                    break;
                case MusicBotMode.Music:
                    _botMode = mode;
                    break;
            }
        }
        
        private SoundRequestType DetectSoundType(string soundName)
        {
            if (soundName.Contains("youtube"))
            {
                return SoundRequestType.Youtube;
            }
            return SoundRequestType.Mp3File;
        }
    }
}
