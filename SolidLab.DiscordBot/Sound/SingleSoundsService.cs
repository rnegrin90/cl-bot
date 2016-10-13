using System;
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
        private readonly WaveFormat _waveFormat;

        public SingleSoundsService(AudioService audioService)
        {
            _audioService = audioService;

            // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.
            _waveFormat = new WaveFormat(48000, 16, _audioService.Config.Channels); 
        }

        public void SetUpCommands (CommandService cmdService)
        {
            cmdService.CreateCommand("sound")
                .Parameter("SoundName")
                .Alias("sd")
                .Description("Play a sound (If found!)")
                .Do(e => Console.WriteLine("Playing sound"));
                //.Do(async e => await PlaySound(e, e.GetArg("SoundName")));

            cmdService.CreateGroup("sound", s =>
            {
                s.CreateCommand("save")
                    .Parameter("Sound", ParameterType.Multiple)
                    //.AddCheck() TODO add check function
                    .Description("Store a sound with its command for it to be used later on!. Usage: ~sd save {sound} {command} [alias]")
                    .Do(e => Console.WriteLine("Adding sound"));
            });
        }

        private async Task PlaySound(CommandEventArgs ev, string soundName)
        {
            var userVoiceChannel = ev.User.VoiceChannel;

            if (userVoiceChannel == null)
            {
                await ev.Channel.SendMessage("You must be in a voice channel to use this command!");
                return;
            }

            if (IsUrl(soundName))
            {
                
            }
            
            try
            {
                var vClient = await _audioService.Join(userVoiceChannel);
                using (var MP3Reader = new Mp3FileReader("./TempResources/AIRHORN.mp3")) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                using (var resampler = new MediaFoundationResampler(MP3Reader, _waveFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
                {
                    resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                    int blockSize = _waveFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
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

        private bool IsUrl(string soundName)
        {
            throw new NotImplementedException();
        }
    }
}
