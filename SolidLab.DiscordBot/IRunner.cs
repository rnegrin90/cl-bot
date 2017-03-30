using System.Threading.Tasks;

namespace SolidLab.DiscordBot
{
    public interface IRunner
    {
        void Run();
        Task Stop();
    }
}