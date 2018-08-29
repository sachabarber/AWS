using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Nito.AsyncEx;

namespace S3BucketsAndKeys
{
    class Program
    {
        // Change the AWSProfileName to the profile you want to use in the App.config file.
        // See http://aws.amazon.com/credentials  for more details.
        // You must also sign up for an Amazon S3 account for this to work
        // See http://aws.amazon.com/s3/ for details on creating an Amazon S3 account
        // Change the bucketName and keyName fields to values that match your bucketname and keyname
        static string bucketName = "some-nice-lower-case-bucket";
        static string keyName = "simpletextkey";
        static IAmazonS3 client;

        private static void Main(string[] args)
        {
            var program = new Program();
            AsyncContext.Run(() => program.MainAsync(args));
        }

        async void MainAsync(string[] args)
        {
            if (CheckRequiredFields())
            {
                using (client = new AmazonS3Client())
                {
                    Console.WriteLine("Listing buckets");
                    await ListingBucketsAsync();

                    Console.WriteLine("Creating a bucket");
                    await CreateABucketAsync(bucketName, false);

                    Console.WriteLine("Writing an object");
                    await WritingAnObjectAsync();

                    Console.WriteLine("Writing an object only first time");
                    await WritingAnObjectOnceAsync();

                    Console.WriteLine("Writing an object using stream and TransferUtility Public availability");
                    await WritingAStreamPublicAsync();

                    Console.WriteLine("Reading an object");
                    await ReadingAnObjectAsync();

                    Console.WriteLine("Deleting an object");
                    await DeletingAnObjectAsync();

                    Console.WriteLine("Listing objects");
                    await ListingObjectsAsync();
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        async Task ListingBucketsAsync()
        {
            await CarryOutAWSTask(async () =>
            {
                ListBucketsResponse response = await client.ListBucketsAsync();
                foreach (S3Bucket bucket in response.Buckets)
                {
                    Console.WriteLine("You own Bucket with name: {0}", bucket.BucketName);
                }
            }, "Listing buckets");
        }

        async Task CreateABucketAsync(string bucketToCreate, bool isPublic = true)
        {
            await CarryOutAWSTask(async () =>
            {
                if (client.DoesS3BucketExist(bucketToCreate))
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
            }, "Create a bucket");
        }

        async Task WritingAnObjectAsync()
        {
            await CarryOutAWSTask(async () =>
            {
                // simple object put
                PutObjectRequest request = new PutObjectRequest()
                {
                    ContentBody = "this is a test",
                    BucketName = bucketName,
                    Key = keyName
                };

                PutObjectResponse response = await client.PutObjectAsync(request);

                // put a more complex object with some metadata and http headers.
                PutObjectRequest titledRequest = new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = keyName
                };
                titledRequest.Metadata.Add("title", "the title");

                await client.PutObjectAsync(titledRequest);
            }, "Writing object");
        }

        async Task WritingAStreamPublicAsync()
        {
            await CarryOutAWSTask(async () =>
            {
                //sly inner function
                async Task<Stream> GenerateStreamFromStringAsync(string s)
                {
                    var stream = new MemoryStream();
                    var writer = new StreamWriter(stream);
                    await writer.WriteAsync(s);
                    await writer.FlushAsync();
                    stream.Position = 0;
                    return stream;
                }

                var bucketToCreate = $"public-{bucketName}";
                await CreateABucketAsync(bucketToCreate);

                var fileTransferUtility = new TransferUtility(client);
                var fileName = Guid.NewGuid().ToString("N");
                using (var streamToUpload = await GenerateStreamFromStringAsync("some random string contents"))
                {
                    var uploadRequest = new TransferUtilityUploadRequest()
                    {
                        InputStream = streamToUpload,
                        Key = $"{fileName}.txt",
                        BucketName = bucketToCreate,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    await fileTransferUtility.UploadAsync(uploadRequest);
                }
                Console.WriteLine($"Upload using stream to file '{fileName}' completed");


            }, "Writing using a Stream to public file");
        }


        async Task WritingAnObjectOnceAsync()
        {
            await CarryOutAWSTask(async () =>
            {
                string uniqueKeyName = Guid.NewGuid().ToString("N");

                for (int i = 0; i < 2; i++)
                {
                    if (!S3FileExists(bucketName, uniqueKeyName))
                    {

                        // simple object put
                        Console.WriteLine($"Adding file {uniqueKeyName}");
                        PutObjectRequest request = new PutObjectRequest()
                        {
                            ContentBody = "this is a test",
                            BucketName = bucketName,
                            Key = uniqueKeyName
                        };

                        PutObjectResponse response = await client.PutObjectAsync(request);
                    }
                    else
                    {
                        Console.WriteLine($"File {uniqueKeyName} existed");
                    }
                }
            }, "Writing object once");
        }


        async Task ReadingAnObjectAsync()
        {
            await CarryOutAWSTask(async () =>
            {
                GetObjectRequest request = new GetObjectRequest()
                {
                    BucketName = bucketName,
                    Key = keyName
                };

                using (GetObjectResponse response = await client.GetObjectAsync(request))
                {
                    string title = response.Metadata["x-amz-meta-title"];
                    Console.WriteLine("The object's title is {0}", title);
                    string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), keyName);
                    if (!File.Exists(dest))
                    {
                        await response.WriteResponseStreamToFileAsync(dest, true, CancellationToken.None);
                    }
                }
            }, "read object");
        }

        async Task DeletingAnObjectAsync()
        {
            await CarryOutAWSTask(async () =>
            {
                DeleteObjectRequest request = new DeleteObjectRequest()
                {
                    BucketName = bucketName,
                    Key = keyName
                };

                await client.DeleteObjectAsync(request);

            }, "delete object");
        }

        async Task ListingObjectsAsync()
        {
            await CarryOutAWSTask(async () =>
            {
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketName;
                ListObjectsResponse response = await client.ListObjectsAsync(request);
                foreach (S3Object entry in response.S3Objects)
                {
                    Console.WriteLine("key = {0} size = {1}", entry.Key, entry.Size);
                }

                // list only things starting with "foo"
                request.Prefix = "foo";
                response = await client.ListObjectsAsync(request);
                foreach (S3Object entry in response.S3Objects)
                {
                    Console.WriteLine("key = {0} size = {1}", entry.Key, entry.Size);
                }

                // list only things that come after "bar" alphabetically
                request.Prefix = null;
                request.Marker = "bar";
                response = await client.ListObjectsAsync(request);
                foreach (S3Object entry in response.S3Objects)
                {
                    Console.WriteLine("key = {0} size = {1}", entry.Key, entry.Size);
                }

                // only list 3 things
                request.Prefix = null;
                request.Marker = null;
                request.MaxKeys = 3;
                response = await client.ListObjectsAsync(request);
                foreach (S3Object entry in response.S3Objects)
                {
                    Console.WriteLine("key = {0} size = {1}", entry.Key, entry.Size);
                }

            }, "listing objects");
        }

        bool CheckRequiredFields()
        {
            NameValueCollection appConfig = ConfigurationManager.AppSettings;

            if (string.IsNullOrEmpty(appConfig["AWSProfileName"]))
            {
                Console.WriteLine("AWSProfileName was not set in the App.config file.");
                return false;
            }
            if (string.IsNullOrEmpty(bucketName))
            {
                Console.WriteLine("The variable bucketName is not set.");
                return false;
            }
            if (string.IsNullOrEmpty(keyName))
            {
                Console.WriteLine("The variable keyName is not set.");
                return false;
            }

            return true;
        }

        bool S3FileExists(string bucketName, string keyName)
        {
            var s3FileInfo = new Amazon.S3.IO.S3FileInfo(client, bucketName, keyName);
            return s3FileInfo.Exists;
        }

        async Task CarryOutAWSTask(Func<Task> taskToPerform, string op)
        {
            try
            {
                await taskToPerform();
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
            }
        }
    }
}