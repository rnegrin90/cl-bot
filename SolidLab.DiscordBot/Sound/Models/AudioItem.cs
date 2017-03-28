using System;
using System.Collections.Generic;
using System.IO;

namespace SolidLab.DiscordBot.Sound.Models
{
    public class AudioItem
    {
        public string Link { get; set; }
        public string SongTitle { get; set; }
        public Stream FileStream { get; set; }
        public string Mp3Path { get; set; }
        public string SoundGroup { get; set; }
        public SoundUse SoundUse { get; set; }
        public List<string> Tags { get; set; }
        public ulong CreatorId { get; set; }
        public TimeSpan Duration { get; set; }
        public SoundRequestType SoundType { get; set; }
    }
}