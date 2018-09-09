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

namespace S3TransferUtility
{
    class Program
    {
        // Change the AWSProfileName to the profile you want to use in the App.config file.
        // See http://aws.amazon.com/credentials  for more details.
        // You must also sign up for an Amazon S3 account for this to work
        // See http://aws.amazon.com/s3/ for details on creating an Amazon S3 account
        // Change the bucketName and keyName fields to values that match your bucketname and keyname
        static string bucketName = "public-some-nice-transferutility-bucket";
        static string fileName = $"{Guid.NewGuid().ToString("N")}.txt";
        static bool deleteBucket = true;
        static bool cleanAllS3FilesAndBuckets = false;
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
                    if(cleanAllS3FilesAndBuckets)
                    {
                        //do some housekeeping, of old buckets
                        await CleanAllBucketsAndFilesAsync();
                    }

                    Console.WriteLine("Writing an object using stream");
                    await WritingAStreamPublicAsync();

                    Console.WriteLine("Reading an object from S3 as stream");
                    await ReadingAnObjectFromS3AsAStream();

                    Console.WriteLine("Downloadinng an object from S3");
                    await DownloadingAnObjectFromS3AsAStream();

                    if(deleteBucket)
                    {
                        Console.WriteLine($"Deleting bucket '{bucketName}'");
                        await client.DeleteObjectAsync(bucketName, fileName);
                        await DeleteBucketAsync();
                    }
                }
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
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

                await CreateABucketAsync(bucketName);

                var fileTransferUtility = new TransferUtility(client);
                var contentsToUpload = "some random string contents";
                Console.WriteLine("Uploading the following contents using TransferUtility");
                Console.WriteLine(contentsToUpload);
                using (var streamToUpload = await GenerateStreamFromStringAsync(contentsToUpload))
                {
                    var uploadRequest = new TransferUtilityUploadRequest()
                    {
                        InputStream = streamToUpload,
                        Key = fileName,
                        BucketName = bucketName,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    //If you are uploading large files, TransferUtility 
                    //will use multipart upload to fulfill the request
                    await fileTransferUtility.UploadAsync(uploadRequest);
                }
                Console.WriteLine($"Upload using stream to file '{fileName}' completed");


            }, "Writing using a Stream to public file");
        }

        async Task ReadingAnObjectFromS3AsAStream()
        {
            await CarryOutAWSTask(async () =>
            {

                var fileTransferUtility = new TransferUtility(client);
                using (var fs = await fileTransferUtility.OpenStreamAsync(bucketName, fileName, CancellationToken.None))
                {
                    using (var reader = new StreamReader(fs))
                    {
                        var contents = await reader.ReadToEndAsync();
                        Console.WriteLine($"Content of file {fileName} is");
                        Console.WriteLine(contents);
                    }
                }
            }, "Reading an Object from S3 as a Stream");
        }

        async Task DownloadingAnObjectFromS3AsAStream()
        {
            await CarryOutAWSTask(async () =>
            {
                var fileTransferUtility = new TransferUtility(client);
                string theTempFile = Path.Combine(Path.GetTempPath(), "SavedS3TextFile.txt");
                try
                {
                    await fileTransferUtility.DownloadAsync(theTempFile, bucketName, fileName);
                    using (var fs = new FileStream(theTempFile, FileMode.Open))
                    {
                        using (var reader = new StreamReader(fs))
                        {
                            var contents = await reader.ReadToEndAsync();
                            Console.WriteLine($"Content of saved file {theTempFile} is");
                            Console.WriteLine(contents);
                        }
                    }
                }
                finally
                {
                    File.Delete(theTempFile);
                }

            }, "Downloading an Object from S3 as a Stream");
        }


        async Task CleanAllBucketsAndFilesAsync()
        {
            ListBucketsResponse response = await client.ListBucketsAsync();
            foreach (S3Bucket bucket in response.Buckets)
            {
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucket.BucketName;
                var responseFile = await client.ListObjectsAsync(request);
                foreach (S3Object entry in responseFile.S3Objects)
                {
                    client.DeleteObject(bucket.BucketName, entry.Key);
                }
                await client.DeleteBucketAsync(bucket.BucketName);
            }
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
            if (string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("The variable fileName is not set.");
                return false;
            }

            return true;
        }

        async Task DeleteBucketAsync()
        {
            await CarryOutAWSTask(async () =>
            {
                await client.DeleteBucketAsync(bucketName);
            }, "delete bucket");
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