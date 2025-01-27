using AWSMonitor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSMonitor.Core.Interfaces
{
    public interface IMetricsService
    {
        Task<IEnumerable<Metric>> GetCPUMetrics(
            string instanceIp,
            DateTime startTime,
            DateTime endTime,
            int interval);
    }
}
