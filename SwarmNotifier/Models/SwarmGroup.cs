using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmNotifier.Models
{
    public class SwarmGroup
    {
        public string Id { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string LdapConfig { get; set; } = string.Empty;
        public string LdapSearchQuery { get; set; } = string.Empty;
        public string LdapUserAttribute { get; set; } = string.Empty;
        public List<string> Subgroups { get; set; } = new();
        public List<string> Owners { get; set; } = new();
        public List<string> Users { get; set; } = new(); 
        public string Name { get; set; } = string.Empty;
        public string? EmailAddress { get; set; }
    }

    public class SwarmGroupsData
    {
        public List<SwarmGroup>? Groups { get; set; }
    }
}
