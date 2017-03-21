using System.Threading.Tasks;
using Discord.Commands;

namespace SolidLab.DiscordBot
{
    public interface IUseCommands
    {
        void SetUpCommands(CommandService cmdService);
    }
}