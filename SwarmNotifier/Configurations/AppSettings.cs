namespace SwarmNotifier.Configurations
{
    public class AppSettings
    {
        public SwarmConfiguration? SwarmConfiguration { get; set; }

        public SlackConfiguration? SlackConfiguration { get; set; }

        public SwarmEventConfiguration SwarmEventConfiguration { get; set; } = new();
    }
}
