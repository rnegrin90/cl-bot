using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace SolidLab.DiscordBot.Images
{
    public class ImageHandler : IUseCommands
    {
        private readonly IImageProviderService _imageProviderService;

        public void SetUpCommands(CommandService cmdService)
        {
            cmdService
                .CreateCommand("waifu")
                .Do(e =>
                {
                    _imageProviderService.Get(e, "waifu");
                });
        }
    }
}
