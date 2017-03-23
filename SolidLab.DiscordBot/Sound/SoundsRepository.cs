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
        private readonly CloudTableClient _tableClient;

        public SoundsRepository(CloudStorageAccount storageAccount)
        {
            _tableClient = storageAccount.CreateCloudTableClient();
        }

        public async Task StoreSound(AudioItem data)
        {
            var table = _tableClient.GetTableReference("sounds");
            table.CreateIfNotExists();

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

            var operation = TableOperation.Insert(new SoundEntity
            {
                Id = Guid.NewGuid(),
                PartitionKey = "",
                RowKey = data.SongTitle,
                LastUsed = DateTime.UtcNow,
                SoundUse = (int) SoundUse.Greeting,
                Enabled = true,
                OwnerId = 1,
                Tags = ""
            });

            await table.ExecuteAsync(operation);
        }

        public List<string> GetAvailableSounds()
        {
            throw new System.NotImplementedException();
        }

        public UserGreeting GetPersonalisedUserGreeting(ulong userId)
        {
            throw new System.NotImplementedException();
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