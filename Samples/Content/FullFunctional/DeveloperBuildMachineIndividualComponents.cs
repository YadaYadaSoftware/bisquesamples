using System;
using System.Linq;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.EWSoftware;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Tfs;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.NodeDotOrg;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Redgate;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.VisualStudio;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class DeveloperBuildMachineIndividualComponents : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();

            var subnet = this.AddNew<Vpc>().WithInternetAccess().WithNewSubnet().WithInternetAccessVia<InternetGateway>();

            var instance = GetInstance(subnet, "Node");
            instance.Deployments.Add( new Node());
            instance.Deployments.Add(new CreateAmi(instance));
            instance.Deployments.Single(d => d.Value is SysPrep).Value.WaitCondition.Timeout = TimeSpan.FromHours(5);

            instance = GetInstance(subnet, "TfxCli");
            instance.Deployments.Add( new TfxCli());
            instance.Deployments.Add( new CreateAmi(instance));
            instance.Deployments.Single(d => d.Value is SysPrep).Value.WaitCondition.Timeout = TimeSpan.FromHours(5);

            instance = GetInstance(subnet, "SandCastle");
            instance.Deployments.Add( new SandCastle());
            instance.Deployments.Add(new CreateAmi(instance));
            instance.Deployments.Single(d => d.Value is SysPrep).Value.WaitCondition.Timeout = TimeSpan.FromHours(5);


            instance = GetInstance(subnet, "SmartAssembly");
            instance.Deployments.Add( new SmartAssembly());
            instance.Deployments.Add(new CreateAmi(instance));
            instance.Deployments.Single(d => d.Value is SysPrep).Value.WaitCondition.Timeout = TimeSpan.FromHours(5);

            instance = GetInstance(subnet, "DotNetSdk");
            instance.Deployments.Add( new DotNetFrameworkSdk());
            instance.Deployments.Add(new CreateAmi(instance));
            instance.Deployments.Single(d => d.Value is SysPrep).Value.WaitCondition.Timeout = TimeSpan.FromHours(5);

            instance = GetInstance(subnet, "Vs");
            instance.Deployments.Add( new Vs());
            instance.Deployments.Add(new CreateAmi(instance));
        }

        private Instance GetInstance(Subnet subnet, string key)
        {
            var node = subnet.AddNew<Instance>()
                .WithElasticIp();
            node.Key = key;
            node.BlockDeviceMappings.RootDevice.Size = 50;
            node.ImageId = "ami-3f0c4628";
            node.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            return node;
        }
    }
}

