namespace SwarmNotifier.Models
{
    public class SwarmTestRun
    {
        public int Id { get; set; }
        public int Change { get; set; }
        public int Version { get; set; }
        public string Test { get; set; } = string.Empty;
        public long StartTime { get; set; }
        public long CompletedTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Messages { get; set; } = new();
        public string Url { get; set; } = string.Empty;
    }

    public class SwarmTestRunData
    {
        public List<SwarmTestRun>? TestRuns { get; set; }
    }
}
