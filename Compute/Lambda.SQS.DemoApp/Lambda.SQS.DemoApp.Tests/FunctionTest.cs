using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.SQSEvents;

using Lambda.SQS.DemoApp;
using Amazon.S3;
using Amazon.S3.Model;

namespace Lambda.SQS.DemoApp.Tests
{
    public class FunctionTest
    {
        private static string bucketName = "lamda-sqs-demo-app-out-bucket";


        [Fact]
        public async Task TestSQSEventLambdaFunction()
        {
            var sqsEvent = new SQSEvent
            {
                Records = new List<SQSEvent.SQSMessage>
                {
                    new SQSEvent.SQSMessage
                    {
                        Body = "foobar"
                    }
                }
            };

            var logger = new TestLambdaLogger();
            var context = new TestLambdaContext
            {
                Logger = logger
            };

            var countBefore = await CountOfItemsInBucketAsync(bucketName);

            var function = new Function();
            await function.FunctionHandler(sqsEvent, context);

            var countAfter = await CountOfItemsInBucketAsync(bucketName);

            Assert.Contains("Processed message foobar", logger.Buffer.ToString());

            Assert.Equal(1, countAfter - countBefore);
        }


        private async Task<int> CountOfItemsInBucketAsync(string bucketName)
        {
            using (var client = new AmazonS3Client(Amazon.RegionEndpoint.EUWest2))
            {
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketName;
                ListObjectsResponse response = await client.ListObjectsAsync(request);
                return response.S3Objects.Count;
            }
        }
    }
}
