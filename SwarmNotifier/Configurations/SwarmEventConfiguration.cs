using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmNotifier.Configurations
{
    public class SwarmEventConfiguration
    {
        public string UpvoteIcon { get; set; } = ":upvote:";
        public string DownvoteIcon { get; set; } = ":downvote:";
        public string CommittedIcon { get; set; } = ":rocket:";
        public string TestPass { get; set; } = ":green_circle:";
        public string TestFail { get; set; } = ":red_circle:";
        public string Archived { get; set; } = ":open_book:";
        public string Approved { get; set; } = ":check:";
        public string Rejected { get; set; } = ":cross:";
        public string NeedsRevision { get; set; } = ":pencil:";
        public string NeedsReview { get; set; } = ":eyes:";
        public string FilesUpdated { get; set; } = ":arrows_clockwise:";
    }
}
