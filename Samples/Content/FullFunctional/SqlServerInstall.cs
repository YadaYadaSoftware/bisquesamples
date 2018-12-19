using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SqlServerInstall : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithInternetAccessVia<InternetGateway>()
                .WithNewInstance()
                .WithElasticIp();
            i.BlockDeviceMappings.RootDevice.Size = 60;
            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            var d = i.Deployments.Add( new SqlServer());
            d.InstallFromPrepared = true;
            i.ImageIdDeploymentDemands.Add(d.DemandName);
            i.BlockDeviceMappings.Add(new BlockDeviceMapping { Size = 50 });
            i.BlockDeviceMappings.Add(new BlockDeviceMapping { Size = 50 });
            i.BlockDeviceMappings.Add(new BlockDeviceMapping { Size = 50 });
        }
    }
}
