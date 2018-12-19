using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.Iam.Roles;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class JustALambaExecutionRole : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<LambdaExecutionRole>();
            i.AssumeRolePolicyDocument.Statement.Principal.Service.Add("x.amazonaws.com");
        }
    }
}