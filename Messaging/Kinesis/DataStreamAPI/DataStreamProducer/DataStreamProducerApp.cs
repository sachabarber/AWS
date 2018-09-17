using Amazon.Kinesis.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.Kinesis.DataStreamproducer
{
    /// <summary>
    /// A sample producer of Kinesis records.
    /// </summary>
    class ProducerApp
    {


        private static readonly AmazonKinesisClient kinesisClient = 
            new AmazonKinesisClient(RegionEndpoint.EUWest2);
        const string myStreamName = "myTestStream";

        public static void Main(string[] args)
        {
            new ProducerApp().WriteToStream().GetAwaiter().GetResult();
        }

        private async Task WriteToStream()
        {
            const string myStreamName = "myTestStream";
            const int myStreamSize = 1;

            try
            {
                var createStreamRequest = new CreateStreamRequest();
                createStreamRequest.StreamName = myStreamName;
                createStreamRequest.ShardCount = myStreamSize;
                var createStreamReq = createStreamRequest;

                var existingStreams = await kinesisClient.ListStreamsAsync();

                if (!existingStreams.StreamNames.Contains(myStreamName))
                {

                    var CreateStreamResponse = await kinesisClient.CreateStreamAsync(createStreamReq);
                    Console.WriteLine("Created Stream : " + myStreamName);
                }
            }
            catch (ResourceInUseException)
            {
                Console.Error.WriteLine("Producer is quitting without creating stream " + myStreamName +
                    " to put records into as a stream of the same name already exists.");
                Environment.Exit(1);
            }

            await WaitForStreamToBecomeAvailableAsync(myStreamName);

            Console.Error.WriteLine("Putting records in stream : " + myStreamName);
            // Write 10 UTF-8 encoded records to the stream.
            for (int j = 0; j < 10; ++j)
            {
                byte[] dataAsBytes = Encoding.UTF8.GetBytes("testdata-" + j);
                using (MemoryStream memoryStream = new MemoryStream(dataAsBytes))
                {
                    try
                    {
                        PutRecordRequest requestRecord = new PutRecordRequest();
                        requestRecord.StreamName = myStreamName;
                        requestRecord.PartitionKey = "url-response-times";
                        requestRecord.Data = memoryStream;

                        PutRecordResponse responseRecord = 
                            await kinesisClient.PutRecordAsync(requestRecord);
                        Console.WriteLine("Successfully sent record to Kinesis. Sequence number: {0}", 
                            responseRecord.SequenceNumber);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to send record to Kinesis. Exception: {0}", ex.Message);
                    }
                }
            }

            Console.ReadLine();

        }

        /// <summary>
        /// This method waits a maximum of 10 minutes for the specified stream to become active.
        /// <param name="myStreamName">Name of the stream whose active status is waited upon.</param>
        /// </summary>
        private static async Task WaitForStreamToBecomeAvailableAsync(string myStreamName)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(10);
            while (DateTime.UtcNow < deadline)
            {
                DescribeStreamRequest describeStreamReq = new DescribeStreamRequest();
                describeStreamReq.StreamName = myStreamName;
                var describeResult = await kinesisClient.DescribeStreamAsync(describeStreamReq);
                string streamStatus = describeResult.StreamDescription.StreamStatus;
                Console.Error.WriteLine("  - current state: " + streamStatus);
                if (streamStatus == StreamStatus.ACTIVE)
                {
                    return;
                }
                Thread.Sleep(TimeSpan.FromSeconds(20));
            }

            throw new Exception("Stream " + myStreamName + " never went active.");
        }


    }
}