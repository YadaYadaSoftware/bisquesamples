using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Oracle;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class MySqlTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            var i = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithInternetAccessVia<InternetGateway>()
                .WithNewInstance()
                .WithElasticIp();
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            i.Deployments.Add( new MySql());

        }
    }
}
