using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Parameters.Psuedo;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class CodeStarTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var project = this.AddNew<CodeStar.Project>();
            project.Properties.ProjectId = "idxyz";
            project.Properties.ProjectName = "namexyz";
            project.Properties.ProjectTemplateId = "arn:aws:codestar:us-east-1::project-template/webservice-netcore-ec2";
            project.Properties.StackId = new StackId();
        }
    }
}