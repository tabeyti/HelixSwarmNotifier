using Serilog;
using SwarmNotifier.Models;
using System.Text.Json;

namespace SwarmNotifier.Services
{
    public class SwarmMonitor
    {
        private readonly SwarmService _swarmService;
        private readonly SlackHelper _slackHelper;
        private Dictionary<int, SwarmReview> _cache = new();

        private readonly string CACHE_FILE = Path.Combine(
            AppContext.BaseDirectory,
            "review-cache.json");

        public SwarmMonitor(SwarmService swarmService, SlackHelper slackHelper)
        {
            _swarmService = swarmService;
            _slackHelper = slackHelper;
        }

        public async Task Run()
        {
            // Prepare cached reviews with the latest reviews retrieved from Swarm
            await PrepareCache();

            // Poll latest reviews, updating our cache with changes, and notifying
            // on specific updates
            while (true)
            {
                await Task.Delay(5000);
                List<SwarmReview>? latestReviews = await _swarmService.GetReviews();
                if (null == latestReviews)
                {
                    Log.Logger.Error("Issue retrieving Swarm reviews.");
                    return;
                }
                await UpdateReviews(latestReviews);
                SaveLocalCache();
            }
        }

        private async Task PrepareCache()
        {
            if (!File.Exists(CACHE_FILE))
            {
                File.WriteAllText(CACHE_FILE, "[]");
            }

            // Grab stored cached reviews from file
            string jsonString = System.IO.File.ReadAllText(CACHE_FILE);
            var cachedReviews = JsonSerializer.Deserialize<List<SwarmReview>>(jsonString);
            if (null == cachedReviews)
            {
                Log.Logger.Error("Could not deserialized cached reviews");
                return;
            }

            // Grab latest reviews and populate our cache with these,
            // transferring over saved Slack messages from the file-stored reviews
            List<SwarmReview>? latestReviews = await _swarmService.GetReviews();
            if (null == latestReviews) return;
            foreach (var review in latestReviews)
            {
                var cachedReview = cachedReviews.SingleOrDefault(r => r.Id == review.Id);
                if (null != cachedReview)
                {
                    review.SlackMessage = cachedReview.SlackMessage;
                }
                _cache.Add(review.Id, review);
            }
        }

        private void SaveLocalCache()
        {
            string jsonString = JsonSerializer.Serialize(_cache.Values, new JsonSerializerOptions()
            {
                WriteIndented = true,
            });
            File.WriteAllText(CACHE_FILE, jsonString);
        }

        private async Task UpdateReviews(List<SwarmReview> latestReviews)
        {
            foreach (var review in latestReviews)
            {
                double minutesSinceCreate = DateTime.Now.ToLocalTime().Subtract(
                    DateTimeOffset.FromUnixTimeSeconds(review.Created).DateTime.ToLocalTime()).TotalMinutes;

                if (!_cache.ContainsKey(review.Id))
                {
                    _cache.Add(review.Id, review);
                }

                // If this is a new review created in the last X minutes, create new message in channel.
                // We do X last minutes to ignore older reviews pulled in after reviews are obliterated,
                // and/or to account for restarts to the service.
                if (minutesSinceCreate <= 2 && _cache[review.Id].SlackMessage == null)
                {
                    Log.Logger.Information($"New Review {review.Id}");
                    await _slackHelper.CreateReviewMessage(review);
                    continue;
                }

                // Assign the cached Slack message (if any) to this review
                review.SlackMessage = _cache[review.Id].SlackMessage;

                // If this review has been recently updated, replace our cached review with this
                // and notify on certain changes
                if (_cache[review.Id].Updated != review.Updated)
                {
                    // A new revision was made if changes are greater than 2 (one for original CL, another for the shelved CL created by P4)
                    if (review.Changes.Count > 2 && review.Changes.Count != _cache[review.Id].Changes.Count)
                    {
                        int latestChange = review.Changes.Max();
                        Log.Logger.Information($"Review {review.Id}: New version {latestChange}");
                        await _slackHelper.ReplyNewVersion(review);
                    }

                    // Review state has updated
                    if (review.State != _cache[review.Id].State)
                    {
                        Log.Logger.Information($"Review {review.Id}: State changed to {review.State}");
                        await _slackHelper.ReplyReviewStateChange(review);
                    }

                    // Test status has updated
                    if (review.TestStatus != _cache[review.Id].TestStatus)
                    {
                        Log.Logger.Information($"Review {review.Id}: Test status changed to {review.TestStatus}");
                        await _slackHelper.ReplyTestStatusChange(review);
                    }

                    // Change(s) committed
                    if (review.Commits.Count > _cache[review.Id].Commits.Count)
                    {
                        Log.Logger.Information($"Review {review.Id}: Change committed in {review.Commits.First()}");
                        await _slackHelper.ReplyChangeCommitted(review);
                    }

                    // Evaluate vote changes
                    foreach (var vote in review.Votes)
                    {
                        // If vote is stale, don't notify
                        if (vote.IsStale) continue;

                        // If this is a new vote or the existing vote value or version has changed, notify
                        var cachedVote = _cache[review.Id].Votes.SingleOrDefault(v => v.User == vote.User);
                        if (null == cachedVote || vote.Value != cachedVote.Value || vote.Version != cachedVote.Version)
                        {
                            Log.Logger.Information($"Review {review.Id}: Vote changed for {vote.User}");
                            await _slackHelper.ReplyVoteChange(review, vote);
                        }
                    }
                }
            }
            // Update our cached reviews with the latest reviews
            _cache.Clear();
            latestReviews.ForEach(r => _cache.Add(r.Id, r));
        }
    }
}
