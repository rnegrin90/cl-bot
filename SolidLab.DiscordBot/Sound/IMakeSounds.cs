using System.Threading.Tasks;
using Discord.Commands;

namespace SolidLab.DiscordBot.Sound
{
    public interface IMakeSounds
    {
        Task Play(CommandEventArgs ev, string soundName);
    }
}