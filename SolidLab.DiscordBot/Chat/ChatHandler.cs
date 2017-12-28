using System;
using Discord.Commands;

namespace SolidLab.DiscordBot.Chat
{
    public class ChatHandler : IUseCommands
    {
        public void SetUpCommands(CommandService cmdService)
        {
            cmdService.CreateCommand("roll")
                  .Description("Rolls a random number between 0 and " + int.MaxValue)
                  .Do(e => e.Channel.SendMessage(Roll()));
        }

        [Command("roll")]
        [Summary("Rolls a random number between 0 and 65000")]
        public string Roll()
        {
            var random = new Random().Next(0, int.MaxValue);
            var last = random % 10;
            var auxAlmost = random % 100;
            var almostLast = auxAlmost / 10;

            if (almostLast - last == 1 || last - almostLast == 1)
            {
                return "YORA DE TRISTESA" + random;
            }

            if (almostLast - last == 0 || last - almostLast == 0)
            {
                return "BRO PILLASTE DUBS " + random;
            }

            return random.ToString();
        }
    }
}
