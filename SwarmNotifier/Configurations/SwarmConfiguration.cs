namespace SwarmNotifier.Configurations
{
    public class SwarmConfiguration
    {
        public string Uri { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ApiUri => $"{Uri}/api/{ApiVersion}";
    }

}
