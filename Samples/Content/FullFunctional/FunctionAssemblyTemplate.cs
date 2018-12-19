using System.Reflection;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.Lambda;
using YadaYada.Bisque.Aws.System.IO;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class FunctionAssemblyTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            //https://console.aws.amazon.com/s3/buckets/aws-codestar-us-east-1-768033286672-bisque-pipe/bisque-Pipeline/bisque-Bui/?region=us-east-1
            FunctionAssembly.AddAssembly(this, new WindowsFileInfo(this.GetType().GetTypeInfo().Assembly.Location));
        }
    }
}
