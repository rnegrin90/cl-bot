using System.IO;

namespace SolidLab.DiscordBot.Images
{
    public class ImageFile : ImageResult
    {
        public FileStream FileStream { get; set; }

        public ImageFile()
        {
            Result = ImageResultType.File;
        }
    }
}