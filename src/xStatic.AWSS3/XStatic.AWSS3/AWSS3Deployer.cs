using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using XStatic.Core;
using XStatic.Core.Deploy;

namespace XStatic.AWSS3
{
    public class AWSS3Deployer : IDeployer
    {
        public const string DeployerKey = "awss3";
        private readonly string _bucketName;
        private readonly string _awsAccessKey;
        private readonly string _awsSecretKey;
        private readonly string _region;
        private readonly bool _emptyBucket;

        public AWSS3Deployer(Dictionary<string, string> parameters)
        {
            _bucketName = parameters["BucketName"];
            _awsAccessKey = parameters["AccessKey"];
            _awsSecretKey = parameters["SecretKey"];
            _region = parameters["Region"];
            _emptyBucket = !string.IsNullOrEmpty(parameters["EmptyBucket"]);
        }

        public virtual async Task<XStaticResult> DeployWholeSite(string folderPath)
        {
            return Deploy(folderPath);
        }

        public virtual XStaticResult Deploy(string folderPath)
        {
            using IAmazonS3 client = getS3Client();
            if (client == null)
            {
                return XStaticResult.Error("Error deploying the site using AWS S3 deploy. Unable to get S3 client.");
            }


            if (_emptyBucket)
            {
                var isDeleted = deleteExistingContents(client, _bucketName).Result;
                if (!isDeleted)
                {
                    return XStaticResult.Error("Error deploying the site using AWS S3 deploy. Unable to delete existing contents.");
                }
            }

            try
            {
                TransferUtilityUploadDirectoryRequest request = new TransferUtilityUploadDirectoryRequest()
                {
                    BucketName = _bucketName,
                    Directory = folderPath,
                    CannedACL = S3CannedACL.NoACL,
                    SearchOption = SearchOption.AllDirectories,
                    SearchPattern = "*.*"
                };

                TransferUtility transferUtility = new TransferUtility(client);

                transferUtility.UploadDirectoryAsync(request).Wait();
            }
            catch (AmazonS3Exception s3Ex)
            {
                return XStaticResult.Error("Error deploying the site using AWS S3 deploy.", s3Ex);
            }
            catch (Exception e)
            {
                return XStaticResult.Error("Error deploying the site using AWS S3 deploy.", e);
            }

            return XStaticResult.Success("Site deployed using AWS S3 deploy.");
        }

        /// <summary>
        /// Instantiate and return an S3 client
        /// </summary>
        /// <returns>S3 client</returns>
        private IAmazonS3 getS3Client()
        {
            RegionEndpoint regionEndpoint = RegionEndpoint.GetBySystemName(_region);
            if (regionEndpoint == null)
            {
                return null;
            }

            return new AmazonS3Client(_awsAccessKey, _awsSecretKey, regionEndpoint);
        }


        /// <summary>
        /// Delete all of the objects stored in an existing Amazon S3 bucket.
        /// </summary>
        /// <param name="client">An initialized Amazon S3 client object.</param>
        /// <param name="bucketName">The name of the bucket from which the
        /// contents will be deleted.</param>
        /// <returns>A boolean value that represents the success or failure of
        /// deleting all of the objects in the bucket.</returns>
        private async Task<bool> deleteExistingContents(IAmazonS3 s3Client, string bucketName)
        {
            // Iterate over the contents of the bucket and delete all objects.
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
            };

            try
            {
                var response = await s3Client.ListObjectsV2Async(request);

                do
                {
                    response.S3Objects
                        .ForEach(obj => s3Client.DeleteObjectAsync(bucketName, obj.Key).Wait());

                    // If the response is truncated, set the request ContinuationToken
                    // from the NextContinuationToken property of the response.
                    request.ContinuationToken = response.NextContinuationToken;
                }
                while (response.IsTruncated);

                return true;
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error deleting objects: {ex.Message}");
                return false;
            }
        }
    }
}
