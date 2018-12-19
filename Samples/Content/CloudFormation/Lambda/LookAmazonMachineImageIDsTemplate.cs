using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Outputs;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.Lambda
{
    public class LookAmazonMachineImageIDsTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            var c = this.AddNew<GetAmiId>();
            c.AmiDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            var o = new Output("AMIID") {Value = new GetAttributeFunction(c.Key, "AmiId")};
            this.Outputs.Add(o);
            this.AddNew<Instance>().ImageId = c.Result;
        }
    }
}
