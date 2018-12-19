using YadaYada.Bisque.Aws.CloudFormation;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class GetAmiId : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<Lambda.AmiLookup.GetAmiId>();
        }
    }
}