using XStatic.Core.Deploy;

namespace XStatic.AWSS3
{
    public class AWSS3AutoInstaller : IDeployerAutoInstaller
    {
        public IDeployerDefinition Definition => new AWSS3DeployerDefinition();

        public Func<Dictionary<string, string>, IDeployer> Constructor => (x) => new AWSS3Deployer(x);
    }
}