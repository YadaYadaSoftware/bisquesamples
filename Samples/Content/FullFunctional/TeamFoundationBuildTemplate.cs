using System.Linq;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Build;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.EWSoftware;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.NodeDotOrg;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Redgate;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.VisualStudio;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Iam.Roles;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class TeamFoundationBuildTemplate : Template
    {
        // this is not a full functional template - it is for short-circuit testing
        // of a tfs build server
        protected override void InitializeTemplate()
        {

            base.InitializeTemplate();
            var i = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();
            i.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 100);
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            var install = i.Deployments.Add(new InstallTfxTask(
                "http://tfs.com:8080/Collection",
                "http://example.com",
                "Bisque",
                "tim",
                "password"));
            i.Deployments.Add( new Node());
            i.Deployments.Add( new SandCastle());
            i.Deployments.Add( new SmartAssembly());
            i.Deployments.Add( new Vs());
            var build = i.Deployments.Add( new TeamFoundationBuild());
            i.AddNew<Role>();
            i.InstanceProfile.Roles.First().Policies.Add(new CloudFormationPolicyProperties());
        }
    }
}
