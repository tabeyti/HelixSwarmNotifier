using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmNotifier.Models
{
    public class SwarmUser
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Update { get; set; } = string.Empty;
        public string Access { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? JobView { get; set; }
        public string? Password { get; set; }
        public string AuthMethod { get; set; } = string.Empty;
        public List<SwarmReview> Reviews { get; set; } = new();
    }

    public class SwarmUsersData
    {
        public List<SwarmUser>? Users { get; set; }
    }
}
