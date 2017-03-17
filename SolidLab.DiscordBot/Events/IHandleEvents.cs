using Discord;

namespace SolidLab.DiscordBot.Events
{
    public interface IHandleEvents
    {
        void EventGenerated(object obj, UserUpdatedEventArgs ev);
    }
}