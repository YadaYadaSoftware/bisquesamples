using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Conditions;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Networking
{
    public class RemoteDesktopSecurityGroupTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var c = this.Conditions.AddNew<Condition>();
            c.Value = new EqualsFunction("x", "x");

            Vpc importedVpc = Vpc.Import("Xyz-Vpc");
            var sg = this.AddNew<RemoteDesktopSecurityGroup>();
            sg.Vpc = importedVpc;
            Subnet subnet1 = Subnet.Import("Subnet1");
            var sql = this.Add(new Instance("Sql") {Subnet = subnet1 });
            sql.SecurityGroups.Add(sg);
            sql.Condition = c;
        }
    }
}
