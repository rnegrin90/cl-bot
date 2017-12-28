using System.Linq;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3;

namespace SolidLab.DiscordBot.Sound
{
    public class SoundSearchService : ISearchSounds
    {
        private readonly YouTubeService _youTubeService;

        public SoundSearchService(YouTubeService youTubeService)
        {
            _youTubeService = youTubeService;
        }

        public async Task<string> Search(string searchString)
        {
            var request = _youTubeService.Search.List("snippet");
            request.Q = searchString;
            request.Type = "video";
            request.MaxResults = 1;

            var response = await request.ExecuteAsync();

            var result = response.Items.FirstOrDefault();

            return GetYoutubeUrlFormat(result?.Id.VideoId);
        }

        private string GetYoutubeUrlFormat(string videoId)
        {
            if (videoId == null)
                return null;

            return $"https://youtu.be/{videoId}";
        }
    }
}