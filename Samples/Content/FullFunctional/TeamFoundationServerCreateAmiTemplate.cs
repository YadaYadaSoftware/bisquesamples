using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Build;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class TeamFoundationServerCreateAmiTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<Vpc>()
                    .WithInternetAccess()
                    .WithNewSubnet()
                    .WithNewInstance()
                    .WithElasticIp();
            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64; ;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            i.Deployments.Add( new DnsServerTools());
            i.Deployments.Add( new TeamFoundationServerApplicationTierInstall());
            i.Deployments.Add(new CreateAmi(i));
        }
    }
}
