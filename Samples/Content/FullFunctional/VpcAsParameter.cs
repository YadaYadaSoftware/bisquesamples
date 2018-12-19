using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    //http://stackoverflow.com/questions/42672459/webserverinstance-encountered-unsupported-property-vpcid
    public class VpcAsParameter : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var v = this.Add(new VpcParameter("VPCSelection"));
            var sg1 = this.AddNew<SecurityGroup>();
            sg1.Vpc = v;
            var i = this.AddNew<Instance>();
            i.SecurityGroups.Add(sg1);

        }
    }
}
