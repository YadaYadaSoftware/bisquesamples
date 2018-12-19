using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class Deploy2TheCloudDirectory : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithSimpleAd();
        }
    }
}
