using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.CloudFormation.Parameters.Psuedo;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Rds;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class RdsMySqlTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();

            var vpc = this.AddNew<Vpc>()
                .WithInternetAccess();

            vpc.EnableDnsHostnames = true;
            vpc.EnableDnsSupport = true;
            var subnet1 = vpc.AddNew<Subnet>();
            subnet1.AvailabilityZone = new SelectFunction(0, new AvailabilityZonesFunction());

            var subnet2 = vpc.AddNew<Subnet>();
            subnet2.AvailabilityZone = new SelectFunction(1, new AvailabilityZonesFunction());

            var additionalDbServerCidrIp = this.Add( new Parameter("AdditionalDbInstanceCidrIp"));
            additionalDbServerCidrIp.Type = ParameterType.String;
            additionalDbServerCidrIp.Label = "Additional CIDR to add to MySQL (for Marketplace)";
            additionalDbServerCidrIp.Default = "8.8.8.8/32";

            // rds DbServer
            var dbServerSubnetGroup = this.AddNew<DbSubnetGroup>();
            dbServerSubnetGroup.Description = "Group for Marketplace MySQL Rds instance";
            dbServerSubnetGroup.Subnets.Add(subnet1);
            dbServerSubnetGroup.Subnets.Add(subnet2);
            var dbServer = this.AddNew<DbInstance>();
            dbServer.PubliclyAccessible = true;
            dbServer.DbSubnetGroup = dbServerSubnetGroup;
            dbServer.Engine = Engine.MySql;

            var dbServerSecurityGroup = subnet1.Vpc.AddNew<SecurityGroup>();
            dbServerSecurityGroup.GroupDescription = "Allows DbServer Access";

            dbServerSecurityGroup.SecurityGroupIngresses.Add(new Ingress()
            {
                FromPort = Port.MySql,
                ToPort = Port.MySql,
                IpProtocol = Protocol.Tcp
            });
            dbServerSecurityGroup.SecurityGroupIngresses.Add(new Ingress()
            {
                CidrIp = additionalDbServerCidrIp,
                FromPort = Port.MySql,
                ToPort = Port.MySql,
                IpProtocol = Protocol.Tcp
            });

            dbServer.VpcSecurityGroups.Add(dbServerSecurityGroup);

        }
    }
}
