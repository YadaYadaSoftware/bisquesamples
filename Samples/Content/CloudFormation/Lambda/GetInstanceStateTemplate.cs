using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Outputs;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.Lambda
{
    public class GetInstanceStateTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            var v = this.AddNew<Vpc>();
            var s = v.AddNew<Subnet>();
            var i = s.AddNew<Instance>();
            i.ImageId = "ami-ee7805f9";

            var f = this.AddNew<GetInstanceState>();
            var c = this.AddNew<GetInstanceState.GetInstanceStateCustom>();

            c.ServiceToken = new GetAttributeFunction(f.Key, GetAttributeFunction.Attributes.Arn);
            c.Properties.Instance = i;
            var o = new Output("InstanceState") { Value = new GetAttributeFunction(c.Key, "InstanceState") };
            this.Outputs.Add(o);
        }
    }
}
