using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.Iam.Roles;
using YadaYada.Bisque.Aws.Lambda;
using YadaYada.Bisque.Aws.Sns;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SnsTopicTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var lamba = this.AddNew<Function>();
            lamba.Handler = "BubbleBoy.Functions::Functions.AcesImport::NewFileUploaded";
            lamba.Role = this.AddNew<LambdaExecutionRole>();
            lamba.Role.AssumeRolePolicyDocument.Statement.Principal.Service.Add("lambda.amazonaws.com");
            lamba.Code = new Function.S3FunctionCode() {S3Bucket = "bubbleboy-768033286672", S3Key = "pipeline-bubbleboy-i/Build-Arti/rb5Jgl2" };
            lamba.Runtime = Function.FunctionRuntime.DotNetCore20;
            var topic = this.AddNew<Topic>();
            var subscription = this.AddNew<SubscriptionForLambdaFunction>();
            subscription.Properties.Endpoint = lamba;
            subscription.Properties.Topic = topic;
            var permission = this.AddNew<Permission>();
            permission.Action = "lambda:InvokeFunction";
            permission.Principal = "sns.amazonaws.com";
            permission.SourceArn = topic;
            permission.FunctionName = new GetAttributeFunction(lamba,GetAttributeFunction.Attributes.Arn);
        }
    }
}
