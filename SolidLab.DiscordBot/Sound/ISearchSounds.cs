using System.Threading.Tasks;

namespace SolidLab.DiscordBot.Sound
{
    public interface ISearchSounds
    {
        Task<string> Search(string searchString);
    }
}