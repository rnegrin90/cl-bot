using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SolidLab.DiscordBot.Sound.Models;

namespace SolidLab.DiscordBot.Sound
{
    class SoundsRepository : ISoundsRepository
    {
        private readonly CloudTable _soundTable;
        private readonly IDownloadAudio _mp3Downloader;
        private readonly IDownloadAudio _youtubeDownloader;
        private readonly IManageBlob _blobManager;

        public SoundsRepository(
            CloudStorageAccount storageAccount, 
            IDownloadAudio mp3Downloader, 
            IDownloadAudio youtubeDownloader, 
            IManageBlob blobManager)
        {
            _mp3Downloader = mp3Downloader;
            _youtubeDownloader = youtubeDownloader;
            _blobManager = blobManager;
            var tableClient = storageAccount.CreateCloudTableClient();
            _soundTable = tableClient.GetTableReference("sounds");
            _soundTable.CreateIfNotExists();
        }

        public async Task StoreSound(AudioItem data, SoundUse usage)
        {
            /*
             * The forward slash (/) character
             * The backslash (\) character
             * The number sign (#) character
             * The question mark (?) character
             * Control characters from U+0000 to U+001F, including:
             * The horizontal tab (\t) character
             * The linefeed (\n) character
             * The carriage return (\r) character
             * Control characters from U+007F to U+009F
             */

            var soundEntity = new SoundEntity
            {
                Id = Guid.NewGuid(),
                PartitionKey = data.SoundType.ToString(),
                RowKey = data.SongTitle,
                LastUsed = DateTime.UtcNow,
                SoundUse = (int) usage,
                Enabled = true,
                OwnerId = data.CreatorId,
                Tags = data.Link,
                Duration = data.Duration.ToString("c")
            };

            var operation = TableOperation.Insert(soundEntity);
            await _soundTable.ExecuteAsync(operation).ConfigureAwait(false);

            await _blobManager.StoreBlob(soundEntity.Id, BlobType.Mp3, data.FileStream).ConfigureAwait(false);
        }

        public List<string> GetAvailableSounds()
        {
            throw new System.NotImplementedException();
        }

        public UserGreeting GetPersonalisedUserGreeting(ulong userId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<AudioItem> GetAudioItem(string sound, SoundRequestType soundType, ulong userId)
        {
            var tableQuery = new TableQuery<SoundEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, soundType.ToString()));

            foreach (var soundEntity in _soundTable.ExecuteQuery(tableQuery))
            {
                if (soundEntity.Tags == sound)
                {
                    soundEntity.LastUsed = DateTime.UtcNow;
                    var updateOperation = TableOperation.Replace(soundEntity);
                    await _soundTable.ExecuteAsync(updateOperation);

                    return await GetAudioFromAzure(soundEntity).ConfigureAwait(false);
                }
            }

            return await DownloadAudioItem(sound, soundType, userId).ConfigureAwait(false);
        }

        private async Task<AudioItem> GetAudioFromAzure(SoundEntity soundEntity)
        {
            var audioStream = await _blobManager.GetBlob(soundEntity.Id, BlobType.Mp3);

            audioStream.Position = 0;

            SoundRequestType soundType;
            Enum.TryParse(soundEntity.PartitionKey, false, out soundType);

            return new AudioItem
            {
                CreatorId = soundEntity.OwnerId,
                FileStream = audioStream,
                Link = soundEntity.Tags,
                SongTitle = soundEntity.RowKey,
                SoundType = soundType
            };
        }

        private async Task<AudioItem> DownloadAudioItem(string sound, SoundRequestType soundType, ulong userId)
        {
            IDownloadAudio selectedDownloader = null;
            switch (soundType)
            {
                case SoundRequestType.LinkMp3:
                    selectedDownloader = _mp3Downloader;
                    break;
                case SoundRequestType.Youtube:
                    selectedDownloader = _youtubeDownloader;
                    break;
            }

            if (selectedDownloader != null)
            {
                var audioData = await selectedDownloader.GetAudioStream(sound).ConfigureAwait(false);

                audioData.CreatorId = userId;
                audioData.SoundType = soundType;
                await StoreSound(audioData, SoundUse.StoredSound).ConfigureAwait(false);

                audioData.FileStream.Position = 0;

                return audioData;
            }

            return null;
        }
    }

    public class SoundEntity : TableEntity
    {
        public Guid Id { get; set; }
        public string Tags { get; set; }
        public ulong OwnerId { get; set; }
        public int SoundUse { get; set; }
        public bool Enabled { get; set; }
        public DateTime LastUsed { get; set; }
        public string Duration { get; set; }

        public SoundEntity() { }
    }

    public enum SoundUse
    {
        Greeting = 1,
        Dismissal = 2,
        StoredSound = 3,
        Meme = 4
    }
}