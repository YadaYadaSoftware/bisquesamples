using System.Linq;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.CodeBuild;
using YadaYada.Bisque.Aws.CodeCommit;
using YadaYada.Bisque.Aws.CodePipeline;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.S3;
using CodeCommitAction = YadaYada.Bisque.Aws.CodePipeline.Stage.CodeCommitAction;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class CodeBuildTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var repositoriesStack = this.Add(new StringParameter("RepostoriesStack") {Default = "repostories"});
            this.Add(new StringParameter("VpcStack") { Default = "bootstrap-database" });

            Vpc vpc = Vpc.Import("${VpcStack}-Vpc");
            Subnet privateSubnet = Subnet.Import("${VpcStack}-PrivateSubnet");
            SecurityGroup dbSecurityGroup = SecurityGroup.Import("${VpcStack}-DbGroup");

            var repo = this.Add(new Repository("Repo1"));


            var pipeline = this.AddNew<Pipeline>("Pipeline");
            pipeline.Properties.ArtifactStore.Location = Bucket.Import("${RepoStack}-BisqueBucket");
            var sourceCodeStage = pipeline.WithSourceCodeStage<CodeCommitAction>();
            sourceCodeStage.Repository = repo;
            sourceCodeStage.Branch = "Xyz";


            CodeBuildStage<NetCoreCodeBuildProject> codeBuildStage = pipeline.WithBuildStage<NetCoreCodeBuildProject>(sourceCodeStage,vpc,new SecurityGroup[] {dbSecurityGroup},new Subnet[] {privateSubnet});

            codeBuildStage.WithEnvironmentVariable("AutoPartsContextUserName", new ImportValue(new Substitute("${VpcStack}-MasterUsername")));
            codeBuildStage.WithEnvironmentVariable("AutoPartsContextDatabase", "AutoParts-development");
            codeBuildStage.WithEnvironmentVariable("AutoPartsContextPassword", new ImportValue(new Substitute("${VpcStack}-MasterUserPassword")));
            codeBuildStage.WithEnvironmentVariable("AutoPartsContextServerName", new ImportValue(new Substitute("${VpcStack}-RdsEndpointAddress")));
            codeBuildStage.WithEnvironmentVariable("AcesVehicleContextUserName", new ImportValue(new Substitute("${VpcStack}-MasterUsername")));
            codeBuildStage.WithEnvironmentVariable("AcesVehicleContextDatabase", "AcesVehicle-development");
            codeBuildStage.WithEnvironmentVariable("AcesVehicleContextPassword", new ImportValue(new Substitute("${VpcStack}-MasterUserPassword")));
            codeBuildStage.WithEnvironmentVariable("AcesVehicleContextServerName", new ImportValue(new Substitute("${VpcStack}-RdsEndpointAddress")));


            Pipeline.Stage<Stage.CreateChangeset,Stage.ExecuteChangeset> deployStage = 
                pipeline.WithCloudFormationDeployStage<Stage.CreateChangeset, Stage.ExecuteChangeset>(codeBuildStage.OutputArtifacts.First());

            Stage.CreateChangeset changeset = deployStage.Actions.First() as Stage.CreateChangeset;
            changeset.Allow(typeof(Lambda.Function));
            changeset.Configuration.Add("ParameterOverrides", $"{{ \"BubbleBoyFunctionsBucket\": {{ \"Fn::GetArtifactAtt\": [ \"{codeBuildStage.OutputArtifacts.First().Name}\", \"BucketName\" ] }}, \"BubbleBoyFunctionsKey\": {{ \"Fn::GetArtifactAtt\": [ \"{codeBuildStage.OutputArtifacts.First().Name}\", \"ObjectKey\" ] }}}}");
            
        }
    }
}