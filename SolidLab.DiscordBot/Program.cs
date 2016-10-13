using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using Castle.Windsor.Installer;

namespace SolidLab.DiscordBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new WindsorContainer();
            container.Install(FromAssembly.This());

            var discordRunner = container.Resolve<BotRunner>();

            Task.Run(() => discordRunner.Run());

            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
