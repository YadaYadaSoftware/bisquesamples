using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Conditions;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Commands;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation
{
    public class ConditionalBatchFileTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();

            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            var cs = i.Init.ConfigSets.AddNew<ConfigSet>();
            var config = cs.AddNew<Config>();

            var c = new Condition("ThisIsAReallyLongConditionNameThatCouldCauseTheLengthToExceed") {
                Value = new AndFunction(
                    new EqualsFunction("a", "b"), 
                    new EqualsFunction("c", "d"))};
            this.Add(c);
            var command = config.Commands.AddNew<Command>();
            command.Condition = c;
            command.Content = "DIR";

            c = new Condition("ThisIsAReallyLongConditionNameThatCouldCauseTheLengthToExceed2")
            {
                Value = new NotFunction(new OrFunction(
                    new EqualsFunction("OrNotEq_InstallBuildOnSqlAndNotEq_Sql4TfsInstanceTypeNotNotEq_TfsInstanceTypeNotNotEq_BuildServerGroupMinSize0", "OrNotEq_InstallBuildOnSqlAndNotEq_Sql4TfsInstanceTypeNotNotEq_TfsInstanceTypeNotNotEq_BuildServerGroupMinSizeZ"), 
                    new EqualsFunction("OrNotEq_InstallBuildOnSqlAndNotEq_Sql4TfsInstanceTypeNotNotEq_TfsInstanceTypeNotNotEq_BuildServerGroupMinSize0", "OrNotEq_InstallBuildOnSqlAndNotEq_Sql4TfsInstanceTypeNotNotEq_TfsInstanceTypeNotNotEq_BuildServerGroupMinSize0")))
            };

            this.Add(c);
            command = config.Commands.AddNew<Command>();
            command.Condition = c;
            command.Content = "DIR c:\\windows";

            c = new Condition("ThisIsAReallyLongConditionNameThatCouldCauseTheLengthToExceed3")
            {
                Value = new NotFunction(new EqualsFunction("i", "j"))
            };
            this.Add(c);
            command = config.Commands.AddNew<Command>();
            command.Condition = c;
            command.Content = "DIR c:\\windows\\system32";



        }
    }
}
