using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Discord;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
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

            container.Register(
                Component
                    .For<YouTubeService>()
                    .UsingFactoryMethod(() => new YouTubeService(new BaseClientService.Initializer
                    {
                        ApiKey = ConfigurationManager.AppSettings.Get("SearchService:YoutubeApiKey"),
                        ApplicationName = "clBot"
                    })),

                Component
                    .For<ISearchSounds>()
                    .ImplementedBy<SoundSearchService>(),

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
                    .ImplementedBy<PlayerService>(),

                Component
                    .For<CloudStorageAccount>()
                    .UsingFactoryMethod(() => CloudStorageAccount.Parse(ConfigurationManager.AppSettings.Get("BlobStorage:ConnectionString"))),

                Component
                    .For<IManageBlob>()
                    .ImplementedBy<BlobManager>(),

                Component
                    .For<ISoundsRepository>()
                    .ImplementedBy<SoundsRepository>()
                    .DependsOn(Dependency.OnComponent("youtubeDownloader", "YoutubeDownloader"))
                    .DependsOn(Dependency.OnComponent("mp3Downloader", "Mp3Downloader")),

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
                    .ImplementedBy<SoundCommandsHandler>()
                    .Named("SoundCommandsHandler"),

                Component
                    .For<IUseCommands>()
                    .ImplementedBy<ChatHandler>()
                    .Named("ChatHandler"),

                Component
                    .For<IRunner>()
                    .ImplementedBy<BotRunner>()
                    .DependsOn(Dependency.OnComponent("soundHandler", "SoundCommandsHandler"))
                    .DependsOn(Dependency.OnComponent("chatHandler", "ChatHandler"))
                    .DependsOn(Dependency.OnComponent("userEventHandler", "EventService"))
                    .DependsOn(Dependency.OnComponent("chatEventHandler", "MessageEventService"))
            );

            container.Register(
            );
        }
    }
}
