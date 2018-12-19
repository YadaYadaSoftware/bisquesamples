using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Outputs;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.CloudFormation.Resources;
using YadaYada.Bisque.Aws.DirectoryService;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Rds;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SqlServerAgainstImportedMicrosoftAd : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            this.Add(new StringParameter("DirectoryStackName"));

            // create the vpc
            Vpc v = Vpc.Import("${DirectoryStackName}-VpcId");
            MicrosoftAd activeDirectory = MicrosoftAd.Import("${DirectoryStackName}-DirectoryAlias", "${DirectoryStackName}-DirectoryName", "${DirectoryStackName}-DirectoryPassword");

            var dbSubnetGroup = this.AddNew<DbSubnetGroup>();
            dbSubnetGroup.Subnets.Add(new Subnet("DbSubnet1") { Vpc = v });
            dbSubnetGroup.Subnets.Add(new Subnet("DbSubnet2") { Vpc = v });

            var db = this.Add(new SqlServerInstance("SqlServer")
            {
                DbSubnetGroup = dbSubnetGroup,
                Engine = Engine.SqlServerExpress,
                Domain = activeDirectory,
                PubliclyAccessible = true,
                DeletionPolicy = Resource.DeletePolicy.Snapshot
            });

            var developmentSecurityGroup = this.AddNew<SecurityGroup>();
            developmentSecurityGroup.Vpc = v;
            developmentSecurityGroup.SecurityGroupIngresses.Add(new Ingress() {CidrIp = "54.165.30.251/32",IpProtocol = Protocol.Tcp, FromPort = Port.MsSqlServer, ToPort = Port.MsSqlServer});
            db.VpcSecurityGroups.Add(developmentSecurityGroup);

            var o = this.Outputs.AddNew<Output>();
            o.Value = new GetAttributeFunction(db,GetAttributeFunction.Attributes.RdsEndpointAddress);
            o.Export.Name = new Substitute("${AWS::StackName}-SqlFqdn");

        }

    }
}
