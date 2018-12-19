using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.Sqs;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SimpleSqsTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            this.AddNew<Queue>();
        }
    }
}
