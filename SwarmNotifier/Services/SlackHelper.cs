using Serilog;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.WebApi;
using SwarmNotifier.Configurations;
using SwarmNotifier.Models;
using System.Threading.Channels;

namespace SwarmNotifier.Services
{
    public class SlackHelper
    {
        private readonly SlackConfiguration _slackConfig;
        private readonly SwarmConfiguration _swarmConfig;
        private readonly SwarmService _swarmService;
        private readonly ISlackApiClient _messageApi;
        private readonly ISlackApiClient _userApi;
        private readonly SwarmEventConfiguration _eventEmojis;

        public SlackHelper(
            SlackConfiguration slackConfig,
            SwarmConfiguration swarmConfig,
            SwarmEventConfiguration eventEmojis,
            SwarmService swarmService)
        {
            _slackConfig = slackConfig;
            _swarmConfig = swarmConfig;
            _swarmService = swarmService;
            _eventEmojis = eventEmojis;

            _messageApi = new SlackServiceBuilder()
               .UseApiToken(_slackConfig.MessageToken)
               .GetApiClient();

            _userApi = new SlackServiceBuilder()
               .UseApiToken(_slackConfig.UserToken)
               .GetApiClient();
        }

        public async Task CreateReviewMessage(SwarmReview review)
        {
            if (null != review.SlackMessage) return;

            var blocks = await CreateParentMessage(review);

            // Send message
            var response = await _messageApi.Chat.PostMessage(new Message
            {
                Channel = _slackConfig.SlackChannel,
                Blocks = blocks
            });

            // Save the Slack message for replies later on
            review.SlackMessage = new SlackMessage
            {
                Channel = response.Channel,
                ThreadTs = response.Ts
            };

            Log.Logger.Information($"Review {review.Id}: Parent message created: {review.SlackMessage}");
        }

        public async Task ReplyNewVersion(SwarmReview swarmReview)
        {
            // If there is no initial Slack channle message to reply to, leave
            await CreateReviewMessage(swarmReview);

            var blocks = new List<Block>
            {
                new SectionBlock
                {
                    Text = new Markdown
                    {
                        Text = $"{_eventEmojis.FilesUpdated} Files updated in review."
                    }
                }
            };

            await _messageApi.Chat.PostMessage(new Message
            {
                Channel = swarmReview?.SlackMessage?.Channel,
                ThreadTs = swarmReview?.SlackMessage?.ThreadTs,
                Blocks = blocks
            });

            Log.Logger.Information($"Review {swarmReview?.Id}: New version reply: {swarmReview?.SlackMessage}");
        }

        public async Task ReplyReviewStateChange(SwarmReview swarmReview)
        {
            await CreateReviewMessage(swarmReview);

            string slackIcon = swarmReview.State switch
            {
                "archived" => _eventEmojis.Archived,
                "approved" => _eventEmojis.Approved,
                "rejected" => _eventEmojis.Rejected,
                "needsRevision" => _eventEmojis.NeedsRevision,
                "needsReview" => _eventEmojis.NeedsReview,
                _ => string.Empty
            };

            var blocks = new List<Block>
            {
                new SectionBlock
                {
                    Text = new Markdown
                    {
                        Text = $"{slackIcon} Review state changed to *{swarmReview?.StateLabel?.ToUpper()}*"
                    } 
                }
            };

            await _messageApi.Chat.PostMessage(new Message
            {
                Channel = swarmReview?.SlackMessage?.Channel,
                ThreadTs = swarmReview?.SlackMessage?.ThreadTs,
                Blocks = blocks
            });

            Log.Logger.Information($"Review {swarmReview?.Id}: Review state changed reply: {swarmReview?.SlackMessage}");
        }

        public async Task ReplyTestStatusChange(SwarmReview swarmReview)
        {
            // Ignore running status
            if (swarmReview.TestStatus == "running") return;

            await CreateReviewMessage(swarmReview);

            SwarmTestRun? testRun = (await _swarmService.GetReviewLatestTestRuns(swarmReview.Id))?.FirstOrDefault();
            if (null == testRun)
            {
                Console.Error.WriteLine($"Could not locate test runs for review {swarmReview.Id}");
                return;
            }

            string slackIcon = swarmReview.TestStatus switch
            {
                "pass" => _eventEmojis.TestPass,
                "fail" => _eventEmojis.TestFail,
                _ => string.Empty
            };
            string testLink = ToSlackLink(testRun.Url, $"Test run v{testRun.Version}");
            string message = $"{slackIcon} {testLink} changed to *{swarmReview.TestStatus?.ToUpper()}*";
            var blocks = new List<Block>
            {
                new SectionBlock { Text = new Markdown { Text = message } }
            };

            await _messageApi.Chat.PostMessage(new Message
            {
                Channel = swarmReview?.SlackMessage?.Channel,
                ThreadTs = swarmReview?.SlackMessage?.ThreadTs,
                Blocks = blocks
            });

            Log.Logger.Information($"Review {swarmReview?.Id}: Test status changed reply: {swarmReview?.SlackMessage}");
        }

        public async Task ReplyChangeCommitted(SwarmReview swarmReview)
        {
            await CreateReviewMessage(swarmReview);

            int commit = swarmReview.Commits.First();
            string commitLink = ToSlackLink($"{_swarmConfig.Uri}/changes/{commit}", commit.ToString());
            var blocks = new List<Block>
            {
                new SectionBlock
                {
                    Text = new Markdown
                    {
                        Text = $"{_eventEmojis.CommittedIcon} Changes committed to {commitLink}"
                    } 
                }
            };

            await _messageApi.Chat.PostMessage(new Message
            {
                Channel = swarmReview?.SlackMessage?.Channel,
                ThreadTs = swarmReview?.SlackMessage?.ThreadTs,
                Blocks = blocks
            });

            Log.Logger.Information($"Review {swarmReview?.Id}: Change committed reply: {swarmReview?.SlackMessage}");
        }

        public async Task ReplyVoteChange(SwarmReview swarmReview, SwarmParticipantDataVote vote)
        {
            await CreateReviewMessage(swarmReview);

            string voteText = vote.Value switch
            {
                1 => $"{_eventEmojis.UpvoteIcon} {vote.User} voted up",
                -1 => $"{_eventEmojis.DownvoteIcon} {vote.User} voted down",
                _ => $"{vote.User} changed vote"
            };

            var blocks = new List<Block>
            {
                new SectionBlock
                {
                    Text = new Markdown
                    {
                        Text = $"{voteText} (revision {vote.Version})"
                    }
                }
            };

            await _messageApi.Chat.PostMessage(new Message
            {
                Channel = swarmReview?.SlackMessage?.Channel,
                ThreadTs = swarmReview?.SlackMessage?.ThreadTs,
                Blocks = blocks
            });

            Log.Logger.Information($"Review {swarmReview?.Id}: Vote change reply: {swarmReview?.SlackMessage}");
        }

        private async Task<string> GetSlackIdForUser(string username)
        {
            try
            {
                // Attempt to retrieve the review using the assigned email first
                string? email = (await _swarmService.GetUser(username))?.Email;
                if (email != null)
                {
                    var response = await _userApi.Users.LookupByEmail(email);
                    if (null != response) return response.Id;
                }
            }
            catch (Exception) {}

            foreach (var emailDomain in _slackConfig.AdditionalDomainsForLookupByEmail)
            {
                try
                {
                    var response = await _userApi.Users.LookupByEmail($"{username}@{emailDomain}");
                    if (null == response) continue;
                    return response.Id;

                }
                catch (Exception) { continue; }
            }
            return username;
        }

        private async Task<IList<Block>> CreateParentMessage(SwarmReview review)
        {
            var blocks = new List<Block>();

            // Slack has a block limit of 150 characters, so truncate.
            string header = review.Description.Split('\n').First();
            if (header.Length >= 150)
            {
                header = new string(header.Take(145).ToArray()) + "...";
            }
            // Add first line in review description as Header
            blocks.Add(new HeaderBlock { Text = header });

            // Add a review link
            string reviewLink = ToSlackLink($"{_swarmConfig.Uri}/reviews/{review.Id}", $"Review {review.Id}");
            blocks.Add(new ContextBlock
            {
                Elements = new List<IContextElement>
                {
                    new Markdown { Text = reviewLink }
                }
            });

            // Add @ mention of the author for this review
            string mentionsMessage = $"*Author:* {ToMention(await GetSlackIdForUser(review.Author))}";

            // Gather all unique participants/reviewers (if any)
            var participants = new HashSet<string>();
            foreach (string participant in review.Participants)
            {
                // If this is a group, grab all group users
                if (participant.Contains("swarm-group-"))
                {
                    string groupId = participant.Replace("swarm-group-", "");
                    var group = _swarmService.Groups?.SingleOrDefault(g => g.Id == groupId);
                    if (null == group) continue;
                    group.Users.ForEach(u => participants.Add(u));
                }
                else
                {
                    participants.Add(participant);
                }
            }
            // Remove author from participants list
            participants.Remove(review.Author);

            // Create @ mentions for each unique participant found
            var reviewerMentions = new List<string>();
            foreach (string user in participants)
            {
                string? slackId = await GetSlackIdForUser(user);
                if (null == slackId) continue;
                reviewerMentions.Add($"<@{slackId}>");
            }
            if (reviewerMentions.Count > 0)
            {
                mentionsMessage += $" - *Reviewers:* {string.Join(", ", reviewerMentions)}";
            }
            blocks.Add(new ContextBlock
            {
                Elements = new List<IContextElement>
                {
                    new Markdown { Text = mentionsMessage }
                }
            });
            return blocks;
        }

        private string ToSlackLink(string link, string text) => 
            $"<{link}|{text}>";

        private string ToMention(string id) =>
            $"<@{id}>";
    }
}
