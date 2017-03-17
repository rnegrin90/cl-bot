using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace SolidLab.DiscordBot.Functions
{
    class ChatHandler : IUseCommands
    {
        public void SetUpCommands(CommandService cmdService)
        {
            
           
            cmdService.CreateCommand("roll")
                  .Description("Rolls a random number between 0 and " + Int32.MaxValue)
                  .Do(e => e.Channel.SendMessage(Roll()));
           

        }
        public string Roll()
        {
            int random = new Random().Next(0, Int32.MaxValue);
            int last = random % 10;
            int auxAlmost = random % 100;
            int almostLast = auxAlmost / 10;
            if (almostLast - last == 1 || last - almostLast == 1)
            {
                return "YORA DE TRISTESA" + random;
            }
            if (almostLast - last == 0 || last - almostLast == 0)
            {
                return "BRO PILLASTE DUBS " + random;
            }
            else
            {
                return random.ToString();
            }
            

            
        }
    }
}
