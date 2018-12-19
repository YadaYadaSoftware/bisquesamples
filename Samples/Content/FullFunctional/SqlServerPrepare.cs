using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SqlServerPrepare : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();

            i.BlockDeviceMappings.RootDevice.Size = 60;
            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            i.Deployments.Add( new DnsServerTools());
            i.Deployments.Add( new SqlServer() {PrepareOnly = true});
            i.Deployments.Add(new CreateAmi(i));
        }
    }
}
