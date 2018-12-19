using System.Linq;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Outputs;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.CloudFormation.Resources;
using YadaYada.Bisque.Aws.DirectoryService;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Iam;
using YadaYada.Bisque.Aws.Iam.Policies;
using YadaYada.Bisque.Aws.Iam.Roles;
using YadaYada.Bisque.Aws.Rds;
using YadaYada.Bisque.Aws.S3;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SqlServerWithRestoreFromS3 : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();

            this.Add(new StringParameter("DirectoryStackName"));

            var bucket = this.AddNew<Bucket>();

            var listAndGetPolicy = new Policy.PolicyProperties("BucketListGetLocationPolicy");
            listAndGetPolicy.Document.Statement.Add(new Statement()
            {
                Resource = new JoinFunction(JoinFunction.DelimiterChar.None, "arn:aws:s3:::", new ReferenceFunction(bucket)),
                Action = { "s3:ListBucket", "s3:GetBucketLocation" }
            });

            var getObjectPolicy = new Policy.PolicyProperties("BucketGetObjectPolicy");
            getObjectPolicy.Document.Statement.Add(new Statement()
            {
                Resource = new JoinFunction(JoinFunction.DelimiterChar.None,
                "arn:aws:s3:::", new ReferenceFunction(bucket), "/*"),
                Action = { "s3:GetObjectMetaData", "s3:GetObject", "s3:PutObject", "s3:ListMultipartUploadParts", "s3:AbortMultipartUpload" }
            });

            var role = this.Add(new Role("RoleToAllowSqlRdsRestoreFromBucket")
            {
                Policies = { listAndGetPolicy, getObjectPolicy }
            });
            role.AssumeRolePolicyDocument.Statement.Principal.Service.Add("rds.amazonaws.com");

            // create the vpc
            Vpc v = Vpc.Import("${DirectoryStackName}-VpcId");
            MicrosoftAd activeDirectory = MicrosoftAd.Import("${DirectoryStackName}-DirectoryAlias", "${DirectoryStackName}-DirectoryName", "${DirectoryStackName}-DirectoryPassword");
            var routeTable1 = RouteTable.Import("${DirectoryStackName}-RouteTable1");

            var dbSubnetGroup = this.AddNew<DbSubnetGroup>();
            dbSubnetGroup.Subnets.Add(new Subnet("DbSubnet1") { Vpc = v, RouteTable = routeTable1 });
            dbSubnetGroup.Subnets.Add(new Subnet("DbSubnet2") { Vpc = v, RouteTable = routeTable1 });
            var options = this.Add(new Rds.OptionGroup("SqlServerWithBackup"));
            options.Properties.EngineName = Engine.SqlServerExpress;
            options.Properties.MajorEngineVersion = "13.00";
            options.Properties.OptionGroupDescription = "SqlServerWithBackup";
            var backupRestore =
                new OptionGroup.OptionGroupProperties.OptionConfiguration()
                {
                    OptionName = "SQLSERVER_BACKUP_RESTORE",
                    Settings =
                    {
                        new OptionGroup.OptionGroupProperties.OptionConfiguration.OptionSettings.Option()
                        {
                            Name = "IAM_ROLE_ARN",
                            Value = new GetAttributeFunction(role.Key, GetAttributeFunction.Attributes.Arn)
                        }
                    }
                };

            options.Properties.Configurations.Add(backupRestore);

            var db = this.Add(new SqlServerInstance("SqlServer")
            {
                DbSubnetGroup = dbSubnetGroup,
                Engine = Engine.SqlServerExpress,
                Domain = activeDirectory,
                PubliclyAccessible = true,
                OptionGroup = options,
                StorageType = VolumeType.GeneralPurpose,
                DeletionPolicy = Resource.DeletePolicy.Snapshot
            });

            var cidr1 = this.Parameters.Add(new StringParameter("SqlServerCidr1")
            {
                Label = "CIDR for SqlServer Access (1)",
                Description = "Allow direct access to SQL Server",
                GroupLabel = db.Parameters.First().Value.GroupLabel
            });

            var cidr2 = this.Parameters.Add(new StringParameter("SqlServerCidr2")
            {
                Label = "CIDR for SqlServer Access (2)",
                Description = "Allow direct access to SQL Server",
                GroupLabel = db.Parameters.First().Value.GroupLabel
            });

            var developmentSecurityGroup = this.AddNew<SecurityGroup>();
            developmentSecurityGroup.Vpc = v;
            developmentSecurityGroup.SecurityGroupIngresses.Add(new Ingress() { CidrIp = cidr1, IpProtocol = Protocol.Tcp, FromPort = Port.MsSqlServer, ToPort = Port.MsSqlServer });
            developmentSecurityGroup.SecurityGroupIngresses.Add(new Ingress() { CidrIp = cidr2, IpProtocol = Protocol.Tcp, FromPort = Port.MsSqlServer, ToPort = Port.MsSqlServer });
            db.VpcSecurityGroups.Add(developmentSecurityGroup);

            this.Outputs.Add(new Output("SqlFqdn")
            {
                Value = new GetAttributeFunction(db, GetAttributeFunction.Attributes.RdsEndpointAddress),
                Export = { Name = new Substitute("${AWS::StackName}-SqlFqdn") }
            });

            this.Outputs.Add(new Output("SqlUserName")
            {
                Value = db.MasterUsername,
                Export = { Name = new Substitute("${AWS::StackName}-SqlUserName") }
            });

            this.Outputs.Add(new Output("SqlPassword")
            {
                Value = db.MasterUserPassword,
                Export = { Name = new Substitute("${AWS::StackName}-SqlPassword") }
            });

        }

    }
}
