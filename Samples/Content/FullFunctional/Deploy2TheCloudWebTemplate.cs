using System;
using System.Linq;
using YadaYada.Bisque.Aws.AutoScaling;
using YadaYada.Bisque.Aws.CertificateManager;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Conditions;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.CloudFormation.Parameters.Psuedo;
using YadaYada.Bisque.Aws.CloudFormation.Resources;
using YadaYada.Bisque.Aws.DirectoryService;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.ElasticLoadBalancingV2;
using YadaYada.Bisque.Aws.Iam;
using YadaYada.Bisque.Aws.Iam.Roles;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;
using YadaYada.Bisque.Aws.Rds;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class Deploy2TheCloudWebTemplate : Template
    {
        protected override void InitializeTemplate()
        {

            var subdomain = this.Add(new StringParameter("HostedZoneSubdomain") {Default = ""});
            var defaultSubdomain = this.Add(new StringParameter("HostedZoneDefaultSubdomain") {Default = "master"});
            StringParameter hostedZoneName = this.Add(new StringParameter("HostedZoneName"));
            this.Add(new StringParameter("DirectoryStackName"));
            this.Add(new StringParameter("SqlStackName"));

            var vpc = Vpc.Import("${DirectoryStackName}-VpcId");
            var directory = MicrosoftAd.Import("${DirectoryStackName}-DirectoryAlias",
                "${DirectoryStackName}-DirectoryName", "${DirectoryStackName}-DirectoryPassword");

            var routeTable1 = RouteTable.Import("${DirectoryStackName}-RouteTable1");
            var subnet1 = this.Add(new Subnet("WebSubnet1")
            {
                Vpc = vpc,
                RouteTable = routeTable1,
                AvailabilityZone = new SelectFunction(0, new AvailabilityZonesFunction())
            });

            var routeTable2 = RouteTable.Import("${DirectoryStackName}-RouteTable2");
            var subnet2 = this.Add(new Subnet("WebSubnet2")
            {
                Vpc = vpc,
                RouteTable = routeTable2,
                AvailabilityZone = new SelectFunction(1, new AvailabilityZonesFunction())
            });

            var deploy2TheCloudGroup = this.AddNew<AutoScalingGroup>();
            var policy = new AutoScalingGroup.AutoScalingRollingUpdate()
            {
                MaxBatchSize = 1,
                MinInstancesInService = 1,
                WaitOnResourceSignals = true,
                PauseTime = "PT1H"
            };

            deploy2TheCloudGroup.UpdatePolicy = policy;
            deploy2TheCloudGroup.VpcZoneIdentifier.Add(subnet1);
            deploy2TheCloudGroup.VpcZoneIdentifier.Add(subnet2);

            var launchConfiguration = deploy2TheCloudGroup.AddNew<LaunchConfiguration>();
            launchConfiguration.AssociatePublicIpAddress = true;
            launchConfiguration.AlwaysReplace = true;
            directory.AddToDomain(launchConfiguration);

            launchConfiguration.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            var s3Policy = new CloudFormationPolicyProperties();
            if (launchConfiguration.InstanceProfile == null)
                launchConfiguration.InstanceProfile = launchConfiguration.AddNew<InstanceProfile>();
            if (launchConfiguration.InstanceProfile.Roles.Count == 0) launchConfiguration.AddNew<Role>();
            launchConfiguration.InstanceProfile.Roles.First().Policies.Add(s3Policy);
            var s3Authentication = new ResourceMetadata.AuthenticationMetadata.S3Authentication();
            s3Authentication.Buckets.Add("d2d2d2d2d2d2");
            s3Authentication.Role = launchConfiguration.InstanceProfile.Roles.First();
            var x = new ResourceMetadata.AuthenticationMetadata();

            launchConfiguration.ResourceMetadata.Add(x.Key, x);
            x.Add("s3rolebased", s3Authentication);

            deploy2TheCloudGroup.LaunchConfiguration = launchConfiguration;
            launchConfiguration.BlockDeviceMappings.Add(new BlockDeviceMapping {Size = 60});
            var lcSg = launchConfiguration.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("RdpGroup");
            lcSg.Vpc = vpc;

            launchConfiguration.Deployments.Add(new WebPlatformInstaller());

            Condition doNotUseSubdomain = this.Add(
                new Condition("DoNotUseSubdomain")
                {
                    Value = new OrFunction(new EqualsFunction(subdomain, ""),
                        new EqualsFunction(subdomain, defaultSubdomain))
                }
            );



            var helpDeployPackage = AddWebDeployPackage(
                "Help",
                doNotUseSubdomain,
                hostedZoneName,
                subdomain,
                deploy2TheCloudGroup,
                new Uri("https://s3.amazonaws.com/d2d2d2d2d2d2/YadaYada/Bisque/latest/WebDeploy/Help"),
                subnet1, subnet2);
            deploy2TheCloudGroup.LaunchConfiguration.Deployments.Add(helpDeployPackage);

            var nugetDeployPackage = AddWebDeployPackage(
                "NugetServer",
                doNotUseSubdomain,
                hostedZoneName,
                subdomain,
                deploy2TheCloudGroup,
                new Uri("https://s3.amazonaws.com/d2d2d2d2d2d2/YadaYada/NugetServer/latest/WebDeploy/NugetServer"),
                subnet1, subnet2);

            var apiKeyParameter = nugetDeployPackage.Add(new Parameter("NuGetServerApiKey")
            {
                Type = ParameterType.String,
                Label = "ApiKey",
                NoEcho = true,
                MinLength = 4,
                Description = "Enter the ApiKey (password) for uploading to the Nuget Server."
            });
            nugetDeployPackage.ParameterValues.Add("ApiKey", apiKeyParameter);

            var nugetPackagesPathParameter = nugetDeployPackage.Add(new Parameter("NugetServerPackagesPath")
            {
                Type = ParameterType.String,
                Label = "Packages Path",
                Default = "c:\\packages"
            });

            nugetDeployPackage.ParameterValues.Add("PackagesPath", nugetPackagesPathParameter);
            deploy2TheCloudGroup.LaunchConfiguration.Deployments.Add(nugetDeployPackage);

            var authenticationServerPackage = AddWebDeployPackage(
                "AuthenticationServer",
                doNotUseSubdomain,
                hostedZoneName,
                subdomain,
                deploy2TheCloudGroup,
                new Uri(
                    "https://s3.amazonaws.com/d2d2d2d2d2d2/YadaYada/InfralutionAuthServer/latest/WebDeploy/AuthenticationServer"),
                subnet1, subnet2);

            authenticationServerPackage.ParameterValues.Add(
                "AuthenticationConnectionString-Web.config Connection String",
                new JoinFunction(JoinFunction.DelimiterChar.SemiColon,
                    new JoinFunction("=", "Server", new ImportValue(new Substitute("${SqlStackName}-SqlFqdn"))),
                    "Database=Authenticate",
                    new JoinFunction("=", "User Id", new ImportValue(new Substitute("${SqlStackName}-SqlUserName"))),
                    new JoinFunction("=", "Password", new ImportValue(new Substitute("${SqlStackName}-SqlPassword"))),
                    "MultipleActiveResultSets=True",
                    "Connect Timeout=120"));

            deploy2TheCloudGroup.LaunchConfiguration.Deployments.Add(authenticationServerPackage);

        }

        private WebPackage AddWebDeployPackage(
            string name,
            Condition doNotUseSubdomain,
            StringParameter domainName, 
            Parameter subdomain, 
            AutoScalingGroup @group, Uri defaultPackageUri, params Subnet[] subnets)
        {

            var packageUri = new Parameter($"{Resource.NormalizeKey(name)}PackageUri")
            {
                Type = ParameterType.String,
                Label = $"Uri to Root of {name} Package",
                Default = defaultPackageUri.AbsoluteUri
            };
            this.Add(packageUri);

            WebPackage package = new WebPackage(packageUri, name);

            var siteName = package.Add(new Parameter($"{Resource.NormalizeKey(name)}SiteName")
            {
                Type = ParameterType.String,
                Label = $"Website Superdomain For {name}",
                Default = name,
                Description = "Do not include FQDN, HostedZone Name, or a period ('.')."
            });

            TargetGroup targetGroup = this.Add(new TargetGroup($"TargetGroup4{name}") { Protocol = TargetGroupProtocol.Http, Port = Port.Http, Vpc = subnets.First().Vpc });
            group.TargetGroups.Add(targetGroup);

            LoadBalancer loadBalancer = package
                .WithLoadBalancer(
                subnets[0],
                subnets[1],
                Port.Http,null,null,
                TargetGroupProtocol.Http, 
                new ProtocolPortMapping()
                {
                    ListenerPort = Port.Http,
                    ListenerProtocol = ListenerProtocol.Https
                });

            var listeners = loadBalancer
                .AddListeners(targetGroup, 
                ListenerProtocol.Https, 
                ListenerProtocol.Http);


            var nameWithoutSubdomainAndWithoutFinalPeriod = new JoinFunction(
                JoinFunction.DelimiterChar.Period,
                siteName,
                domainName);

            var nameWithSubdomainAndWithoutFinalPeriod = 
                new JoinFunction(JoinFunction.DelimiterChar.Period, 
                    new JoinFunction(JoinFunction.DelimiterChar.Period, 
                    siteName, 
                    subdomain), 
                domainName);

            var nameWithoutFinalPeriod = new IfFunction(
                doNotUseSubdomain.Key, 
                nameWithoutSubdomainAndWithoutFinalPeriod, 
                nameWithSubdomainAndWithoutFinalPeriod);

            var recordSet = package.WithDnsRecord(loadBalancer, domainName, nameWithoutFinalPeriod);

            Certificate certificate = listeners
                .Single(l => l.Protocol == ListenerProtocol.Https)
                .WithCertificate(nameWithoutFinalPeriod, domainName);

            package.ParameterValues.Add("IIS Web Application Name", certificate.DomainName);
            package.AttributesFile.Content.Iis.DocumentRoot = "c:/inetpub/wwwroot2";
            package.AttributesFile.Content.WebDeploy.SiteName = certificate.DomainName;

            recordSet.DependsOn.Add(package.WaitCondition);

            return package;
        }



        private DbInstance AddMySql(Subnet subnet1, Subnet subnet2)
        {
            throw new NotImplementedException();
            var additionalDbServerCidrIp = this.Add(new Parameter("AdditionalDbInstanceCidrIp"));
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
            return dbServer;
        }

        //private void AddLicenseServer(Subnet subnet1, Subnet subnet2, LaunchConfiguration launchConfiguration)
        //{
        //    var sqlServer =  AddSqlServer(subnet1, subnet2);
        //    AddWebDeployPackage(sqlServer, launchConfiguration);
        //}

        //private void AddWebDeployPackage(DbInstance sqlServer, LaunchConfiguration launchConfiguration)
        //{
        //    var deployPackageUriParameter = this.Add( new Parameter("LicenseServerWebDeployPackageUri");
        //    deployPackageUriParameter.Type = ParameterType.String;
        //    deployPackageUriParameter.Label = "Uri to Root of Nuget Server Package";
        //    deployPackageUriParameter.Default = "https://s3.amazonaws.com/d2d2d2d2d2d2/YadaYada/InfralutionAuthServer/latest/WebDeploy/AuthenticationServer";

        //    var siteNameParameter = this.Add( new Parameter("SiteName");
        //    siteNameParameter.Type = ParameterType.String;
        //    siteNameParameter.Label = "Name of the License Server Web Site";
        //    siteNameParameter.Default = "Default Web Site/license";

        //    WebDeployPackage deployPackage = new WebDeployPackage(deployPackageUriParameter, "AuthenticationServer");
        //    deployPackage.ParameterValues.Add("IIS Web Application Name", siteNameParameter);
        //    //<connectionStrings>
        //    //    <add name="AuthenticationConnectionString" connectionString="Data Source=is18dzcpeyazcql.cte6jojl9qjb.us-east-1.rds.amazonaws.com;Initial Catalog=Authorize;Persist Security Info=True;User ID='masterusername';Password='Ju83!#3.A*'" providerName="System.Data.OleDb" />

        //    deployPackage.ParameterValues.Add("AuthenticationConnectionString-Web.config Connection String",
        //        new JoinFunction(JoinFunction.DelimiterChar.None,
        //        "Initial Catalog=Authorize;Integrated Security=False;User Id=",
        //        sqlServer.MasterUsername,
        //        ";Password=",
        //        sqlServer.MasterUserPassword,
        //        ";MultipleActiveResultSets=True;Data Source="
        //        , sqlServer.GetEndPoint()));

        //    launchConfiguration.Deployments.Add("LicenseServerWebDeploy", deployPackage);

        //}

        //private DbInstance AddSqlServer(Subnet subnet1, Subnet subnet2)
        //{
        //    var additionalSqlServerCidrIp = this.Add( new Parameter("AdditionalSqlServerCidrIp");
        //    additionalSqlServerCidrIp.Type = ParameterType.String;
        //    additionalSqlServerCidrIp.Label = "Additional CIDR to add to SQLServer (for License Tracker)";
        //    additionalSqlServerCidrIp.Default = "8.8.8.8/32";

        //    // rds sqlServer
        //    var sqlServerSubnetGroup = this.AddNew<DbSubnetGroup>("DbSubnetGroupForSqlInstance");
        //    sqlServerSubnetGroup.Description = "Group for Sql Rds instance";
        //    sqlServerSubnetGroup.Subnets.Add(subnet1);
        //    sqlServerSubnetGroup.Subnets.Add(subnet2);
        //    var sqlServer = this.AddNew<DbInstance>("SqlServer");
        //    sqlServer.PubliclyAccessible = true;
        //    sqlServer.DbSubnetGroup = sqlServerSubnetGroup;
        //    sqlServer.Engine = Engine.SqlServerExpress;



        //    var sqlServerSecurityGroup = subnet1.Vpc.AddNew<SecurityGroup>("DbSecurityGroup");
        //    sqlServerSecurityGroup.GroupDescription = "Allows SQLServer Access";

        //    sqlServerSecurityGroup.SecurityGroupIngresses.Add(new Ingress()
        //    {
        //        FromPort = Port.MsSqlServer,
        //        ToPort = Port.MsSqlServer,
        //        IpProtocol = Protocol.Tcp
        //    });
        //    sqlServerSecurityGroup.SecurityGroupIngresses.Add(new Ingress()
        //    {
        //        CidrIp = additionalSqlServerCidrIp,
        //        FromPort = Port.MsSqlServer,
        //        ToPort = Port.MsSqlServer,
        //        IpProtocol = Protocol.Tcp
        //    });

        //    sqlServer.VpcSecurityGroups.Add(sqlServerSecurityGroup);
        //    return sqlServer;
        //}

       

        
    }
}
