namespace Score2Stream.Core.Settings
{
    public class UserSettings
    {
        #region Public Properties

        public App App { get; set; } = new App();

        public Detection Detection { get; set; } = new Detection();

        public Scoreboard Scoreboard { get; set; } = new Scoreboard();

        public Session Video { get; set; } = new Session();

        #endregion Public Properties
    }
}