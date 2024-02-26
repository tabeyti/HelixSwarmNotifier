using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmNotifier.Configurations
{
    public class SlackConfiguration
    {
        public string? MessageToken { get; set; }
        public string? UserToken { get; set; }
        public string? SlackChannel { get; set; }
        public List<string> AdditionalDomainsForLookupByEmail { get; set; } = new();
    }
}
