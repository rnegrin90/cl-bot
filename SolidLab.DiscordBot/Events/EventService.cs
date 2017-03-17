using Discord;
using Discord.Commands;
using SolidLab.DiscordBot.Sound;
using SolidLab.DiscordBot.Sound.Models;

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

        public void EventGenerated(object obj, UserUpdatedEventArgs ev)
        {
            if (ev.After.VoiceChannel != null)
                _soundService.Play(ev.After.VoiceChannel, ev.After, "AIRHORN", SoundRequestType.Mp3File);
            //_soundsRepository.GetPersonalisedUserGreeting(ev.User.Id);
            //if (_soundsRepository.GetPersonalisedUserGreeting(ev.User.Id) != null)
            //    _soundService.Play(, "");
        }
    }
}