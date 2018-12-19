using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Networking.Vpcing
{
    public class VpcSamples : Template
    {
        protected override void InitializeTemplate()
        {
            // create a new Vpc with a logical id of "MyVpc"
            Vpc vpc = new Vpc("MyVpc");
            // add it to the template
            this.Add(vpc);
            var s = this.AddNew<Subnet>();
            s.Vpc = vpc;
        }
    }
}
