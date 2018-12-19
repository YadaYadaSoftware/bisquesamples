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
    public class AcesImportDevEnvironment : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            //var eip = this.AddNew<ElasticIp>();
            //var natGateway = this.AddNew<NatGateway>();
            //natGateway.ElasticIp = eip;
            this.Add(new StringParameter("DirectoryStackName"));

            // create the vpc
            Vpc importedVpc = Vpc.Import("${DirectoryStackName}-VpcId");
            MicrosoftAd activeDirectory = MicrosoftAd.Import("${DirectoryStackName}-DirectoryAlias", "${DirectoryStackName}-DirectoryName", "${DirectoryStackName}-DirectoryPassword");


            Subnet subnet1 = Subnet.Import("${DirectoryStackName}-Subnet1");
            Subnet subnet2 = Subnet.Import("${DirectoryStackName}-Subnet2");
            subnet1.Vpc = importedVpc;
            subnet2.Vpc = importedVpc;

            var dbSubnetGroup = this.AddNew<DbSubnetGroup>();
            dbSubnetGroup.Subnets.Add(subnet1);
            dbSubnetGroup.Subnets.Add(subnet2);

            var db = this.Add(new SqlServerInstance("SqlServer")
            {
                DbSubnetGroup = dbSubnetGroup,
                Engine = Engine.SqlServerExpress,
                Domain = activeDirectory,
                PubliclyAccessible = true,
                DeletionPolicy = Resource.DeletePolicy.Snapshot
            });

            db.SnapshotId = new StringParameter("DbSnapshotId");

            var developmentSecurityGroup = this.AddNew<SecurityGroup>();
            developmentSecurityGroup.Vpc = importedVpc;
            developmentSecurityGroup.SecurityGroupIngresses.Add(new Ingress() { CidrIp = "54.165.30.251/32", IpProtocol = Protocol.Tcp, FromPort = Port.MsSqlServer, ToPort = Port.MsSqlServer });
            db.VpcSecurityGroups.Add(developmentSecurityGroup);

            var o = this.Outputs.AddNew<Output>();
            o.Value = new GetAttributeFunction(db, GetAttributeFunction.Attributes.RdsEndpointAddress);
            o.Export.Name = new Substitute("${AWS::StackName}-SqlFqdn");

        }

    }
}
