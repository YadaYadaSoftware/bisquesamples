using System;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.CloudFormation.Resources;
using YadaYada.Bisque.Aws.Common;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Git;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Google;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.VisualStudio;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Rds;
using YadaYada.Bisque.Aws.Serverless;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class InstanceFluentTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();

            // create the vpc
            var v = this.AddNew<Vpc>()
                .WithInternetAccess()
                .HavingCidrBlockOf("10.0.0.0/16");

            var dbSubnet1 = v.WithNewSubnet("DbSubnet1")
                .HavingCidrBlockOf("10.0.0.0/24")
                .WithAvailibilityZone(0);

            var dbSubnet2 = v.WithNewSubnet("DbSubnet2")
                .HavingCidrBlockOf("10.0.1.0/24")
                .WithAvailibilityZone(1);

            var databaseAccessGroup = this.Add(new SecurityGroup("Database Access Group")
            {
                GroupDescription = "Allows access to the RDS database",
                Vpc = v

            });

            var dbOptionSet = this.AddNew<OptionGroup>();
            dbOptionSet.Properties.EngineName = "sqlserver-ex";
            dbOptionSet.Properties.MajorEngineVersion = "13.00";
            dbOptionSet.Properties.OptionGroupDescription = "SqlServer Express 2016";

            var dbSubnetGroup = this.AddNew<DbSubnetGroup>();
            dbSubnetGroup.Subnets.Add(dbSubnet1);
            dbSubnetGroup.Subnets.Add(dbSubnet2);

            var db = this.Add(new SqlServerInstance("SqlServerInstance")
            {
                Engine = Engine.SqlServerExpress,
                DeletionPolicy = Resource.DeletePolicy.Delete,
                StorageType = VolumeType.GeneralPurpose,
                DbSubnetGroup = dbSubnetGroup,
                OptionGroup = dbOptionSet
            });


            var dbSecurityGroup = this.Add(new SecurityGroup("DbGroup")
            {
                GroupDescription = "Db Group",
                Vpc = v

            });
            dbSecurityGroup.SecurityGroupIngresses.Add(new Ingress()
            {
                SourceSecurityGroup = databaseAccessGroup,
                IpProtocol = Protocol.Tcp,
                FromPort = Port.MsSqlServer,
                ToPort = Port.MsSqlServer
            });
            db.VpcSecurityGroups.Add(dbSecurityGroup);
            db.SnapshotId = db.Add(new StringParameter("SnapshotId")
            {
                Default = "arn:aws:rds:us-east-1:768033286672:snapshot:b4-modifying-directory"
            });


            var developmentSubnet = v.WithNewSubnet("DevelopmentSubnet")
                .HavingCidrBlockOf("10.0.2.0/24")
                .WithAvailibilityZone(0)
                .WithInternetAccessVia<InternetGateway>();


            var codeBuildPublicSubnet = v.WithNewSubnet("CodeBuildPublicSubnet")
                .HavingCidrBlockOf("10.0.3.0/24")
                .WithAvailibilityZone(0)
                .WithPublicIpAddresses()
                .WithInternetAccessVia<InternetGateway>();

            var gateway = codeBuildPublicSubnet.AddNew<NatGateway>();
            gateway.AddNew<ElasticIp>();

            var codeBuildPrivateSubnet = v.WithNewSubnet("CodeBuildPrivateSubnet")
                .HavingCidrBlockOf("10.0.4.0/24")
                .WithAvailibilityZone(0)
                .WithInternetAccessVia(gateway);

            var e = new ServerlessFunction.ApiGatewayEvent
            {
                Path = "/{proxy+}",
                Method = "ANY"
            };

            var workstation = developmentSubnet.AddNew<Instance>("Workstation");
            workstation.WithRootDeviceOfSize(300);
            workstation.RunningPlatform(Platform.Windows);
            var hup = workstation.Deployments.AddNew<Hup>();
            hup.Deploy(typeof(AwsCli), typeof(SqlServer), typeof(GitGui));

            var vs = hup.Deployments.AddNew<Vs>();
            vs.WithAwsToolkit()
                .WithReSharper()
                .WithPowershellTools()
                .AttributesFile.Content.Vs.Version = "2017";
            hup.Deployments.Add(new Chrome()).WaitCondition.Timeout = new TimeSpan().MaxCloudFormationWait();
            workstation.SecurityGroups.Add(dbSecurityGroup,new RemoteDesktopSecurityGroup("RemoteDesktopSecurityGroup"));
        }
    }
}
