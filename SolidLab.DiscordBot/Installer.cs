using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Discord;

namespace SolidLab.DiscordBot
{
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Classes
                .FromThisAssembly()
                .Pick()
                .LifestyleTransient());

            container.Register(
                Component.For<DiscordClient>()
            );
        }
    }
}
