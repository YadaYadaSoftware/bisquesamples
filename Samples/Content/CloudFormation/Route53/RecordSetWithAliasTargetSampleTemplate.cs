using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.Route53;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.Route53
{
    public class RecordSetWithAliasTargetSampleTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            var recordSet = this.AddNew<ResourceRecordSet>();
            recordSet.ResourceRecords.Add("8.8.8.8");
        }
    }
}
