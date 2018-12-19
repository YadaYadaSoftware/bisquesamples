using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SimpleDirectoryTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            var i = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithSimpleAd()
                .WithNewInstance()
                .WithElasticIp();
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
        }
    }
}
