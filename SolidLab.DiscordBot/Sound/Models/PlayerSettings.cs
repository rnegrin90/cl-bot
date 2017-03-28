using NAudio.Wave;

namespace SolidLab.DiscordBot.Sound.Models
{
    public class PlayerSettings
    {
        public WaveFormat WaveFormat { get; set; }
        public int BlockSize { get; set; }
        public float Volume { get; set; }

        public PlayerSettings(WaveFormat waveFormat)
        {
            WaveFormat = waveFormat;
            BlockSize = waveFormat.AverageBytesPerSecond / 50;
            Volume = 1;
        }
    }
}