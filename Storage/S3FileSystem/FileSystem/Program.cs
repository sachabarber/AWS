using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Text;
using Amazon.S3;
using Amazon.S3.IO;

namespace FileSystem
{
    class Program
    {
        // Change the AWSProfileName to the profile you want to use in the App.config file.
        // See http://aws.amazon.com/credentials  for more details.
        // You must also sign up for an Amazon S3 account for this to work
        // See http://aws.amazon.com/s3/ for details on creating an Amazon S3 account
        // Change the bucketName field to a unique name that will ;be created and used for the sample.
        static string bucketName = Guid.NewGuid().ToString("N").ToLower();
        static IAmazonS3 client;
        static bool deleteAtEnd = false;

        private static void Main(string[] args)
        {
            if (checkRequiredFields())
            {
                using (client = new AmazonS3Client(new AmazonS3Config()
                {
                    MaxErrorRetry = 2,
                    ThrottleRetries = true
                }))
                {
                    // Creates the bucket.
                    S3DirectoryInfo rootDirectory = new S3DirectoryInfo(client, bucketName);
                    rootDirectory.Create();

                    // Creates a file at the root of the bucket.
                    S3FileInfo readme = rootDirectory.GetFile("README.txt");
                    using (StreamWriter writer = new StreamWriter(readme.OpenWrite()))
                        writer.WriteLine("This is my readme file.");

                    // Create a directory called code and write a file to it.
                    S3DirectoryInfo codeDir = rootDirectory.CreateSubdirectory("wiki");
                    S3FileInfo codeFile = codeDir.GetFile("Phantasmagoria.txt");
                    using (StreamWriter writer = new StreamWriter(codeFile.OpenWrite()))
                    {
                        writer.WriteLine("Phantasmagoria (About this sound American pronunciation (help·info), also fantasmagorie, fantasmagoria) was a form of horror theatre that ");
                        writer.WriteLine("(among other techniques) used one or more magic lanterns to project frightening images such as skeletons, demons, and ");
                        writer.WriteLine("ghosts onto walls, smoke, or semi-transparent screens, typically using rear projection to keep the lantern out of sight. Mobile or ");
                        writer.WriteLine("portable projectors were used, allowing the projected image to move and change size on the screen, and multiple projecting ");
                        writer.WriteLine("devices allowed for quick switching of different images. In many shows the use of spooky decoration, total darkness, sound ");
                        writer.WriteLine("effects, (auto-)suggestive verbal presentation and sound effects were also key elements. Some shows added all kinds of ");
                        writer.WriteLine("sensory stimulation, including smells and electric shocks. Even required fasting, fatigue (late shows) and drugs have been ");
                        writer.WriteLine("mentioned as methods of making sure spectators would be more convinced of what they saw. The shows started under the ");
                        writer.WriteLine("guise of actual séances in Germany in the late 18th century, and gained popularity through most of Europe (including Britain) ");
                        writer.WriteLine("throughout the 19th century.");
                    }


                    // Create a directory called license and write a file to it.
                    S3DirectoryInfo licensesDir = rootDirectory.CreateSubdirectory("licenses");
                    S3FileInfo licenseFile = licensesDir.GetFile("license.txt");
                    using (StreamWriter writer = new StreamWriter(licenseFile.OpenWrite()))
                        writer.WriteLine("A license to code");


                    Console.WriteLine("Write Directory Structure");
                    Console.WriteLine("------------------------------------");
                    WriteDirectoryStructure(rootDirectory, 0);


                    Console.WriteLine("\n\n");
                    foreach (var file in codeDir.GetFiles())
                    {
                        Console.WriteLine("Content of {0}", file.Name);
                        Console.WriteLine("------------------------------------");
                        using (StreamReader reader = file.OpenText())
                        {
                            Console.WriteLine(reader.ReadToEnd());
                        }
                    }

                    // Deletes all the files and then the bucket.
                    if(deleteAtEnd)
                        rootDirectory.Delete(true);
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void WriteDirectoryStructure(S3DirectoryInfo directory, int level)
        {
            StringBuilder indentation = new StringBuilder();
            for (int i = 0; i < level; i++)
                indentation.Append("\t");

            Console.WriteLine("{0}{1}", indentation, directory.Name);
            foreach (var file in directory.GetFiles())
                Console.WriteLine("\t{0}{1}", indentation, file.Name);

            foreach (var subDirectory in directory.GetDirectories())
            {
                WriteDirectoryStructure(subDirectory, level + 1);
            }
        }

        static bool checkRequiredFields()
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

            return true;
        }
    }
}
