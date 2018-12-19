using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.EWSoftware;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Git;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Google;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Tfs;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.NodeDotOrg;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Redgate;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.VisualStudio;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class DeveloperBuildMachineCreateAmiTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<Vpc>()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();

            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;

            i.BlockDeviceMappings.RootDevice.Size = 50;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            i.Deployments.Add(new DnsServerTools());
            i.Deployments.Add(new Chrome());
            i.Deployments.Add(new DotNetFrameworkSdk());
            i.Deployments.Add(new GitGui());
            i.Deployments.Add(new SandCastle());
            i.Deployments.Add(new SmartAssembly());
            i.Deployments.Add(new Node());
            i.Deployments.Add(new TfxCli());
            var vs = i.Deployments.AddNew<Vs>()
                .WithAwsToolkit()
                .WithPowershellTools().AttributesFile.Content.Vs.Version = "2017";
            i.Deployments.Add(new AwsCli());
            i.Deployments.AddNew<PowershellTools>();
            i.Deployments.Add(new CreateAmi(i));
        }
    }
}
