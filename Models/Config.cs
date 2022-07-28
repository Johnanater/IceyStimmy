namespace IceyStimmy.Models
{
    public class Config
    {
        public RewardSystem RewardSystem { get; set; }
        
        public int TimeMinutes { get; set; }
        
        public decimal PlayTime { get; set; }
        public decimal PlayerKill { get; set; }
        public decimal AnimalKill { get; set; }
        public decimal ZombieKill { get; set; }
        
        public bool PunishOnDeath { get; set; }
        public decimal DeathAmount { get; set; }

        public ChatConfig ChatConfig { get; set; }
    }
}
