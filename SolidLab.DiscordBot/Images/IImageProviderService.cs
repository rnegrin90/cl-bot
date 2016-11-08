using System.Threading.Tasks;
using Discord.Commands;

namespace SolidLab.DiscordBot.Images
{
    public interface IImageProviderService
    {
        Task Get(CommandEventArgs ev, string cmd);
    }
}