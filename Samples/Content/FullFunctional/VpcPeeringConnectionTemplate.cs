using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class VpcPeeringConnectionTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var peering = this.AddNew<VpcPeeringConnection>();
            var v = this.AddNew<Vpc>();
            var v2 = this.AddNew<Vpc>();
            peering.Vpc = v;
            peering.PeerVpc = v2;
        }
    }
}
