namespace SolidLab.DiscordBot.Sound.Models
{
    public class PlayerStatus
    {
        public InternalStatus Status { get; set; }
        public string StatusMessage { get; set; }
    }

    public enum InternalStatus
    {
        Idle = 0,
        Playing = 1,
        Stopped = 2,
        Paused = 3,
        Starting = 4,
    }
}