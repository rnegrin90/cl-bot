using System;

namespace SolidLab.DiscordBot.Events
{
    public interface IHandleEvents
    {
        void EventGenerated(object obj, EventArgs ev);
    }
}