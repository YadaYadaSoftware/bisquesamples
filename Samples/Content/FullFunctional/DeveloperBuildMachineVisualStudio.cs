using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class DeveloperBuildMachineVisualStudio : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();

            var subnet = this.AddNew<Vpc>().WithInternetAccess().WithNewSubnet();
            var instance = GetInstance(subnet, "DevBuild-Node");
            var createAmi = instance.Deployments.Add(new CreateAmi(instance));
        }

        private Instance GetInstance(ISubnet subnet, string key)
        {
            var node = subnet.AddNew<Instance>();
            node.Key = key;

            node.BlockDeviceMappings.RootDevice.Size = 50;
            node.ImageId = "ami-3f0c4628";
            node.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            return node;
        }
    }
}

