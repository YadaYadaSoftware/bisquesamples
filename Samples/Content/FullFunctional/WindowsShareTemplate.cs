using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows.Share;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class WindowsShareTemplate : Template
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
            var share = i.Deployments.Add(
                new WindowsShare("backup","c:\\backup",i.Subnet.CidrBlock));
        }
    }
}
