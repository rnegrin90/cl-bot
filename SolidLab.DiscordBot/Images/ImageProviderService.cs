using System;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace SolidLab.DiscordBot.Images
{
    public class ImageProviderService : IImageProviderService
    {
        private readonly IImageRepository _imageRepository;

        public ImageProviderService(IImageRepository imageRepository)
        {
            _imageRepository = imageRepository;
        }

        public Task Get(CommandEventArgs ev, string cmd)
        {
            throw new NotImplementedException();
        }
    }

    public interface IImageRepository
    {

    }
}
