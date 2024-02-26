using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SwarmNotifier.Models
{
    public class SwarmResponse<T>
    {
        public string? Error { get; set; }
        public List<string>? Messages { get; set; }
        public T? Data { get; set; }
    }
}
