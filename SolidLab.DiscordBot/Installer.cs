using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Discord;
using Discord.Audio;
using Microsoft.WindowsAzure.Storage;
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
                    .For<IDownloadAudio>()
                    .ImplementedBy<YoutubeDownloader>()
                    .Named("YoutubeDownloader")
                    .DependsOn(Dependency.OnValue("soundQuality", ConfigurationManager.AppSettings.Get("YoutubeDownloader:SoundQuality")))
                    .DependsOn(Dependency.OnValue("soundCache", ConfigurationManager.AppSettings.Get("YoutubeDownloader:CacheFolder"))),

                Component
                    .For<IDownloadAudio>()
                    .ImplementedBy<Mp3Downloader>()
                    .Named("Mp3Downloader")
                    .DependsOn(Dependency.OnValue("soundCache", ConfigurationManager.AppSettings.Get("YoutubeDownloader:CacheFolder"))),

                Component
                    .For<IMakeSounds>()
                    .ImplementedBy<SoundService>()
                    .DependsOn(Dependency.OnComponent("youtubeDownloader", "YoutubeDownloader"))
                    .DependsOn(Dependency.OnComponent("mp3Downloader", "Mp3Downloader")),
                
                Component
                    .For<CloudStorageAccount>()
                    .UsingFactoryMethod(() => CloudStorageAccount.Parse(ConfigurationManager.AppSettings.Get("BlobStorage:ConnectionString"))),

                Component
                    .For<ISoundsRepository>()
                    .ImplementedBy<SoundsRepository>(),

                Component
                    .For<IHandleEvents>()
                    .ImplementedBy<EventService>()
                    .Named("EventService"),

                Component
                    .For<IHandleEvents>()
                    .ImplementedBy<MessageEventService>()
                    .Named("MessageEventService"),

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
                    .DependsOn(Dependency.OnComponent("userEventHandler", "EventService"))
                    .DependsOn(Dependency.OnComponent("chatEventHandler", "MessageEventService"))
            );

            container.Register(
            );
        }
    }
}
