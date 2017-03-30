using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using Castle.Windsor.Installer;

namespace SolidLab.DiscordBot
{
    class Program
    {
        private static IRunner _discordRunner;
        
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            _discordRunner.Stop().Wait();
            return true;
        }

        static void Main(string[] args)
        {
            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);

            var container = new WindsorContainer();
            container.Install(FromAssembly.This());

            _discordRunner = container.Resolve<IRunner>();

            _discordRunner.Run();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
