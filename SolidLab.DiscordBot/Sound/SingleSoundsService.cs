using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Audio;
using Discord.Commands;
using NAudio.Wave;

namespace SolidLab.DiscordBot.Sound
{
    public class SingleSoundsService
    {
        private ISoundsRepository _soundsRepo;
        private readonly AudioService _audioService;

        public SingleSoundsService(AudioService audioService)
        {
            _audioService = audioService;
        }

        public void SetUpCommands (CommandService cmdService)
        {
            cmdService.CreateCommand("sound")
                .Parameter("SoundName")
                .Description("Play a sound (If found!)")
                .Do(async e => await PlaySound(e, e.GetArg("SoundName")));
        }

        private async Task PlaySound(CommandEventArgs ev, string soundName)
        {
            var userVoiceChannel = ev.User.VoiceChannel;

            if (userVoiceChannel == null)
            {
                await ev.Channel.SendMessage("You must be in a voice channel to use this command!");
                return;
            }

            try
            {
                var vClient = await _audioService.Join(userVoiceChannel);
                var OutFormat = new WaveFormat(48000, 16, _audioService.Config.Channels); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
                using (var MP3Reader = new Mp3FileReader("./TempResources/AIRHORN.mp3")) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
                {
                    resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                    int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                    byte[] buffer = new byte[blockSize];
                    int byteCount;

                    while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) // Read audio into our buffer, and keep a loop open while data is present
                    {
                        if (byteCount < blockSize)
                        {
                            // Incomplete Frame
                            for (int i = byteCount; i < blockSize; i++)
                                buffer[i] = 0;
                        }
                        vClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public interface ISoundsRepository
    {
        List<string> GetAvailableSounds();

    }
}
