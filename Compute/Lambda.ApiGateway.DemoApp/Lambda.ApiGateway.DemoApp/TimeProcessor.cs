using System;
using System.Collections.Generic;
using System.Text;

namespace Lambda.ApiGateway.DemoApp
{
    public interface ITimeProcessor
    {
        DateTime CurrentTimeUTC();
    }

    public class TimeProcessor : ITimeProcessor
    {
        public DateTime CurrentTimeUTC()
        {
            return DateTime.UtcNow;
        }
    }
}
