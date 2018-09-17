using Amazon.Kinesis.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.Kinesis.DataStreamConsumer
{
    /// <summary>
    /// A sample producer of Kinesis records.
    /// </summary>
    class ConsumerApp
    {
        private static readonly AmazonKinesisClient kinesisClient = 
            new AmazonKinesisClient(RegionEndpoint.EUWest2);
        const string myStreamName = "myTestStream";

        public static void Main(string[] args)
        {
            new ConsumerApp().ReadFromStream().GetAwaiter().GetResult();
        }

        private async Task ReadFromStream()
        {
            DescribeStreamRequest describeRequest = new DescribeStreamRequest();
            describeRequest.StreamName = myStreamName;

            DescribeStreamResponse describeResponse = 
                await kinesisClient.DescribeStreamAsync(describeRequest);
            List<Shard> shards = describeResponse.StreamDescription.Shards;

            foreach (Shard shard in shards)
            {
                GetShardIteratorRequest iteratorRequest = new GetShardIteratorRequest();
                iteratorRequest.StreamName = myStreamName;
                iteratorRequest.ShardId = shard.ShardId;
                iteratorRequest.ShardIteratorType = ShardIteratorType.TRIM_HORIZON;

                GetShardIteratorResponse iteratorResponse = await kinesisClient.GetShardIteratorAsync(iteratorRequest);
                string iteratorId = iteratorResponse.ShardIterator;

                while (!string.IsNullOrEmpty(iteratorId))
                {
                    GetRecordsRequest getRequest = new GetRecordsRequest();
                    getRequest.Limit = 1000;
                    getRequest.ShardIterator = iteratorId;

                    GetRecordsResponse getResponse = await kinesisClient.GetRecordsAsync(getRequest);
                    string nextIterator = getResponse.NextShardIterator;
                    List<Record> records = getResponse.Records;

                    if (records.Count > 0)
                    {
                        Console.WriteLine("Received {0} records. ", records.Count);
                        foreach (Record record in records)
                        {
                            string theMessage = Encoding.UTF8.GetString(record.Data.ToArray());
                            Console.WriteLine("message string: " + theMessage);
                        }
                    }
                    iteratorId = nextIterator;
                }
            }
        }

    }
}