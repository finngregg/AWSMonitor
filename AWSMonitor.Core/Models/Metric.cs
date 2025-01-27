using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSMonitor.Core.Models
{
    public class Metric
    {
        public DateTime Timestamp { get; set; }
        public double Utilization { get; set; }
        public string? InstanceId { get; set; }
    }
}
