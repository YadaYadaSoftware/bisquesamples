using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Networking.Vpcing
{
    public class SubnetWithNoVpc : Template
    {
        protected override void InitializeTemplate()
        {
            this.AddNew<Subnet>();
        }
    }
}
