using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Lambda.SQS.DemoApp
{
    public class Function
    {
        static IAmazonS3 client;
        private static string bucketName = "lamda-sqs-demo-app-out-bucket";

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {

        }


        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            foreach(var message in evnt.Records)
            {
                await ProcessMessageAsync(message, context);
            }
        }

        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            context.Logger.LogLine($"Processed message {message.Body}");

            using (client = new AmazonS3Client(Amazon.RegionEndpoint.EUWest2))
            {
                Console.WriteLine("Creating a bucket");
                await CreateABucketAsync(bucketName, false);
                Console.WriteLine("Writing message from SQS to bucket");
                await WritingAnObjectAsync(message.Body.ToUpper(), Guid.NewGuid().ToString("N").ToLower());
            }


            // TODO: Do interesting work based on the new message
            await Task.CompletedTask;
        }


        async Task WritingAnObjectAsync(string messageBody, string keyName)
        {
            await CarryOutAWSTask<Unit>(async () =>
            {
                // simple object put
                PutObjectRequest request = new PutObjectRequest()
                {
                    ContentBody = messageBody,
                    BucketName = bucketName,
                    Key = keyName
                };

                PutObjectResponse response = await client.PutObjectAsync(request);
                return Unit.Empty;
            }, "Writing object");
        }


        async Task CreateABucketAsync(string bucketToCreate, bool isPublic = true)
        {
            await CarryOutAWSTask<Unit>(async () =>
            {
                if(await BucketExists(bucketToCreate))
                {
                    Console.WriteLine($"{bucketToCreate} already exists, skipping this step");
                }

                PutBucketRequest putBucketRequest = new PutBucketRequest()
                {
                    BucketName = bucketToCreate,
                    BucketRegion = S3Region.EUW2,
                    CannedACL = isPublic ? S3CannedACL.PublicRead : S3CannedACL.Private
                };
                var response = await client.PutBucketAsync(putBucketRequest);
                return Unit.Empty;
            }, "Create a bucket");
        }


        async Task<bool> BucketExists(string bucketName)
        {
            return await CarryOutAWSTask<bool>(async () =>
            {
                ListBucketsResponse response = await client.ListBucketsAsync();
                return  response.Buckets.Select(x => x.BucketName).Contains(bucketName);
            }, "Listing buckets");
        }

        async Task<T> CarryOutAWSTask<T>(Func<Task<T>> taskToPerform, string op)
        {
            try
            {
                return await taskToPerform();
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine("Please check the provided AWS Credentials.");
                    Console.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
                }
                else
                {
                    Console.WriteLine($"An Error, number '{amazonS3Exception.ErrorCode}', " +
                                      $"occurred when '{op}' with the message '{amazonS3Exception.Message}'");
                }

                return default(T);
            }
        }


    }



    public class Unit
    {
        public static Unit Empty => new Unit();
    }
}
