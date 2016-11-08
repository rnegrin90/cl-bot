namespace SolidLab.DiscordBot.Images
{
    public class ImageLink : ImageResult
    {
        public string Link { get; set; }

        public ImageLink()
        {
            Result = ImageResultType.Link;
        }
    }
}