using System;
using Discord;

namespace SolidLab.DiscordBot.Events
{
    public class MessageEventService : IHandleEvents
    {
        public void EventGenerated(object obj, EventArgs ev)
        {
            var discordEvent = (MessageEventArgs) ev;
            if (discordEvent.Message.Text.ToLower() == "here come dat boi")
            {
                discordEvent.Channel.SendMessage("o shit waddup!");
            }
        }
    }
}
