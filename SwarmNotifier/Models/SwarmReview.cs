using System.Text.Json;
using System.Text.Json.Serialization;

namespace SwarmNotifier.Models
{
    public class SwarmReview
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public List<int> Changes { get; set; } = new();
        public List<int> Commits { get; set; } = new();
        public string Author { get; set; } = string.Empty;
        public List<string> Participants { get; set; } = new();

        private Dictionary<string, JsonElement>? _participantsData;
        public Dictionary<string, JsonElement>? ParticipantsData 
        {
            get => _participantsData;
            set
            {
                _participantsData = value;
                if (null == value) return;
                foreach (KeyValuePair<string, JsonElement> keyVal in value)
                {
                    // If value is an Array, ignore it
                    if (keyVal.Value.ValueKind == JsonValueKind.Array) continue;

                    // Value is an object, but it may be 1 of 3 different object types (Swarm is stupid).
                    // We care only about Vote objects though, so if it's not a Vote, ignore it
                    SwarmParticipantDataVote? vote = keyVal.Value.Deserialize<SwarmParticipantData>(new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })?.Vote;
                    if (null == vote) continue;
                    vote.User = keyVal.Key;
                    Votes.Add(vote);
                }
            }
        }
        /// <summary>
        /// Our internal property for bookeeping review votes. This is populated by
        /// the <see cref="ParticipantsData"/>
        /// </summary>
        [JsonIgnore]
        public List<SwarmParticipantDataVote> Votes { get; set; } = new();
        public int HasReviewer { get; set; }
        public string Description { get; set; } = string.Empty;
        public long Created { get; set; }
        public long Updated { get; set; }
        public string? State { get; set; }
        public string? StateLabel { get; set; }
        public string? TestStatus { get; set; }
        public string? PreviousTestStatus { get; set; }
        public bool? Pending { get; set; }
        public List<string>? Groups { get; set; }
        public Dictionary<string, int>? Complexity { get; set; }

        // Custom Fields

        /// <summary>
        /// Original slack message for this review. This is used for
        /// indicating an initial message for a review thread in Slack
        /// has been created, but also for storing the intial message
        /// details to add follow up reply messages.
        /// </summary>
        public SlackMessage? SlackMessage { get; set; }
    }

    public class SlackMessage
    {
        public string? Channel { get; set; }
        public string? ThreadTs { get; set; }

        public SlackMessage() { }

        public SlackMessage(SlackMessage otherMessage)
        {
            Channel = otherMessage.Channel;
            ThreadTs = otherMessage.ThreadTs;
        }

        public override string ToString() =>
            $"Channel: {Channel} - Thread: {ThreadTs}";
    }

    public class SwarmReviewsData
    {
        public List<SwarmReview>? Reviews { get; set; }
    }

    public class SwarmParticipantData
    {
        public SwarmParticipantDataVote? Vote { get; set; }
    }

    public class SwarmParticipantDataVote
    {
        public string User { get; set; }
        public int Value { get; set; }
        public int Version { get; set; }
        public bool IsStale { get; set; }
    }
}
