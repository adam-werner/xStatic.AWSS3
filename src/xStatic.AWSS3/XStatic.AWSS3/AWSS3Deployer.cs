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
            _emptyBucket = emptyBucketValueCheck(parameters["EmptyBucket"]);
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
                // Establish the min size of the multipart upload
                TransferUtilityConfig transferUtilityConfig = new TransferUtilityConfig()
                {
                    MinSizeBeforePartUpload = 25
                };

                // Instantiate the transfer utility object
                TransferUtility transferUtility = new TransferUtility(client, transferUtilityConfig);

                // Create the upload directory request object
                TransferUtilityUploadDirectoryRequest request = new TransferUtilityUploadDirectoryRequest()
                {
                    BucketName = _bucketName,
                    Directory = folderPath,
                    CannedACL = S3CannedACL.NoACL,
                    SearchOption = SearchOption.AllDirectories,
                    SearchPattern = "*.*"
                };

                // Upload the files
                transferUtility.UploadDirectoryAsync(request).Wait();
            }
            catch (AmazonS3Exception s3Ex)
            {
                return XStaticResult.Error("S3 Exception :: Error deploying the site using AWS S3 deploy.", s3Ex);
            }
            catch (Exception e)
            {
                return XStaticResult.Error("Exception :: Error deploying the site using AWS S3 deploy.", e);
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
        /// Delete all of the objects stored in an existing Amazon S3 bucket using the bulk delete method
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
                ListObjectsV2Response response = await s3Client.ListObjectsV2Async(request);

                if (response != null && response.S3Objects != null && response.S3Objects.Any())
                {
                    var keyVersions = response.S3Objects.Select(x => new KeyVersion { Key = x.Key }).ToList();

                    foreach(var keyVersionsSubset in keyVersions.Chunk(1000))
                    {
                        var multipleObjectsRequest = new DeleteObjectsRequest()
                        {
                            BucketName = bucketName,
                            Objects = keyVersionsSubset.ToList()
                        };

                        s3Client.DeleteObjectsAsync(multipleObjectsRequest).Wait();
                    }
                }

                return true;
            }
            catch (AmazonS3Exception ex)
            {
                // Revisit this to see if logging could be incorporated
                return false;
            }
        }


        /// <summary>
        /// Pass in string value entered by administrator
        /// </summary>
        /// <param name="emptyBucketParam">Front-end configuration value</param>
        /// <returns>Boolean</returns>
        private bool emptyBucketValueCheck(string emptyBucketParam)
        {
            if (string.IsNullOrEmpty(emptyBucketParam))
                return false;

            // Matching values for bucket emptying - y, yes, true, empty
            string[] emptyBucketValues = new string[] { "y", "yes", "true", "empty" };

            return emptyBucketValues.Contains(emptyBucketParam.ToLower());            
        }
    }
}
