namespace SolidLab.DiscordBot.Sound.Models
{
    public class UserGreeting
    {
        public int UserId { get; set; }
        public object Sound { get; set; }
        public SoundRequestType SoundType { get; set; }
    }
}