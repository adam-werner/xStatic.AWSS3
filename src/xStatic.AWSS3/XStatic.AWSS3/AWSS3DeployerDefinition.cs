using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XStatic.Core.Deploy;

namespace XStatic.AWSS3
{
    public class AWSS3DeployerDefinition : IDeployerDefinition
    {
        public string Id => AWSS3Deployer.DeployerKey;

        public string Name => "AWS S3";

        public string Help => "The AWS S3 bucket will be mirrored to match the generated site.";

        public IEnumerable<string> Fields => new[]
        {
            "BucketName",
            "AccessKey",
            "SecretKey",
            "Region",
            "EmptyBucket"
        };
    }
}
