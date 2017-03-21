using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Discord;
using Discord.Audio;
using SolidLab.DiscordBot.Chat;
using SolidLab.DiscordBot.Events;
using SolidLab.DiscordBot.Sound;

namespace SolidLab.DiscordBot
{
    public class Installer : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component
                    .For<DiscordClient>()
                    .LifestyleSingleton()
            );

            var client = container.Resolve<DiscordClient>();

            client.UsingAudio(x => // Opens an AudioConfigBuilder so we can configure our AudioService
            {
                x.Mode = AudioMode.Outgoing; // Tells the AudioService that we will only be sending audio
            });

            container.Register(
                Component
                    .For<IMakeSounds>()
                    .ImplementedBy<SoundService>()
                    .DependsOn(Dependency.OnValue("cacheFolderPath",ConfigurationManager.AppSettings.Get("SoundService:CacheFolder")))
                    .DependsOn(Dependency.OnValue("soundQuality", ConfigurationManager.AppSettings.Get("SoundService:SoundQuality"))),
                
                Component
                    .For<ISoundsRepository>()
                    .ImplementedBy<SoundsRepository>(),

                Component
                    .For<IHandleEvents>()
                    .ImplementedBy<EventService>(),

                Component
                    .For<IUseCommands>()
                    .ImplementedBy<SoundHandler>()
                    .Named("SoundHandler"),

                Component
                    .For<IUseCommands>()
                    .ImplementedBy<ChatHandler>()
                    .Named("ChatHandler"),

                Component
                    .For<IRunner>()
                    .ImplementedBy<BotRunner>()
                    .DependsOn(Dependency.OnComponent("soundHandler", "SoundHandler"))
                    .DependsOn(Dependency.OnComponent("chatHandler", "ChatHandler"))
            );

            container.Register(
            );
        }
    }
}
