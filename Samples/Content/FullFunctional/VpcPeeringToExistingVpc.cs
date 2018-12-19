using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class VpcPeeringToExistingVpc : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var peering = this.AddNew<VpcPeeringConnection>();
        }
    }
}
