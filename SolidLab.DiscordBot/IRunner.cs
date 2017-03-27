using System.Threading.Tasks;

namespace SolidLab.DiscordBot
{
    public interface IRunner
    {
        Task Run();
        Task Stop();
    }
}