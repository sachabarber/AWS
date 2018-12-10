using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.ElastiCacheCluster;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

namespace ElastiCache.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            // instantiate a new client.
            ElastiCacheClusterConfig config = new ElastiCacheClusterConfig();
            MemcachedClient memClient = new MemcachedClient(config);

            // Store the data for 3600 seconds (1hour) in the cluster. 
            // The client will decide which cache host will store this item.
            memClient.Store(StoreMode.Set,"mykey","This is the data value.", TimeSpan.FromMinutes(10));

            var mykey = memClient.Get<string>("mykey");




            Console.ReadLine();
        }
    }
}
