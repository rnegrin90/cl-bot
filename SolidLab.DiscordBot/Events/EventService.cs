using System;
using System.Threading.Tasks;
using Discord;
using SolidLab.DiscordBot.Sound;

namespace SolidLab.DiscordBot.Events
{
    public class EventService : IHandleEvents
    {
        private readonly ISoundsRepository _soundsRepository;
        private readonly IMakeSounds _soundService;

        public EventService(ISoundsRepository soundsRepository, IMakeSounds soundService)
        {
            _soundsRepository = soundsRepository;
            _soundService = soundService;
        }

        public void EventGenerated(object obj, EventArgs ev)
        {
            var discordEvent = (UserUpdatedEventArgs) ev;
            if (discordEvent.After.VoiceChannel != null)
            {
                var audio = _soundsRepository.GetPersonalisedUserGreeting(discordEvent.After.Id).Result;
                if (audio != null)
                {
                    Task.Run(() => _soundService.Play(discordEvent.After.VoiceChannel, discordEvent.After, audio));
                }
            }
            //    _soundService.Play(ev.After.VoiceChannel, ev.After, "AIRHORN", SoundRequestType.Mp3File);
            //_soundsRepository.GetPersonalisedUserGreeting(ev.User.Id);
            //if (_soundsRepository.GetPersonalisedUserGreeting(ev.User.Id) != null)
            //    _soundService.Play(, "");
        }
    }
}