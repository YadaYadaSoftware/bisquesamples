using YadaYada.Bisque.Aws.AutoScaling;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.CloudFormation.Parameters.Psuedo;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.ElasticLoadBalancingV2;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;
using YadaYada.Bisque.Aws.Rds;
using YadaYada.Bisque.Aws.Route53;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class LicenseServerTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();

            var deployPackageUriParameter = this.Add( new Parameter("LicenseServerWebDeployPackageUri"));
            deployPackageUriParameter.Type = ParameterType.String;
            deployPackageUriParameter.Label = "Uri to Root of Nuget Server Package";
            deployPackageUriParameter.Default = "https://s3.amazonaws.com/yadayada/LicenseServerWebDeploy";

            var siteNameParameter = this.Add( new Parameter("SiteName"));
            siteNameParameter.Type = ParameterType.String;
            siteNameParameter.Label = "Name of the License Server Web Site";
            siteNameParameter.Default = "Default Web Site/license";


            var additionalSqlServerCidrIp = this.Add( new Parameter("AdditionalSqlServerCidrIp"));
            additionalSqlServerCidrIp.Type = ParameterType.String;
            additionalSqlServerCidrIp.Label = "Additional CIDR to add to SQLServer (for License Tracker)";
            additionalSqlServerCidrIp.Default = "8.8.8.8/32";

            var hostedZoneName = this.Add( new Parameter("HostedZoneName"));
            hostedZoneName.Type = ParameterType.String;
            hostedZoneName.Label = "Hosted Zone Name";


            var subnet1 = this.AddNew<Subnet>().WithInternetAccessVia<InternetGateway>();
            subnet1.AvailabilityZone = new SelectFunction(0, new AvailabilityZonesFunction());

            var subnet2 = this.AddNew<Subnet>().WithInternetAccessVia<InternetGateway>();
            subnet2.AvailabilityZone = new SelectFunction(1, new AvailabilityZonesFunction());
            

            var certificateArnParameter = new CertificateArnParameter("LoadBalancerCertificateArn", "Load Balancer Certificate")
            {
                Default = "arn:aws:acm:us-east-1:768033286672:certificate/31426cb8-124b-4a1a-a9d2-d4ebb5f2ae22"
            };

            this.Add(certificateArnParameter);
            var targetGroup = this.AddNew<TargetGroup>();
            targetGroup.Protocol = TargetGroupProtocol.Http;
            targetGroup.Port = Port.Http;



            // rds sqlServer
            var sqlServerSubnetGroup = this.AddNew<DbSubnetGroup>();
            sqlServerSubnetGroup.Description = "Group for Sql Rds instance";
            sqlServerSubnetGroup.Subnets.Add(subnet1);
            sqlServerSubnetGroup.Subnets.Add(subnet2);
            var sqlServer = this.AddNew<DbInstance>();
            sqlServer.PubliclyAccessible = true;
            sqlServer.DbSubnetGroup = sqlServerSubnetGroup;
            sqlServer.Engine = Engine.SqlServerExpress;


            var sqlServerSecurityGroup = sqlServer.VpcSecurityGroups.AddNew<SecurityGroup>("DbSecurityGroup");
            sqlServerSecurityGroup.GroupDescription = "Allows SQLServer Access";

            sqlServerSecurityGroup.SecurityGroupIngresses.Add(new Ingress()
            {
                FromPort = Port.MsSqlServer,
                ToPort = Port.MsSqlServer,
                IpProtocol = Protocol.Tcp
            });
            sqlServerSecurityGroup.SecurityGroupIngresses.Add(new Ingress()
            {
                CidrIp = additionalSqlServerCidrIp,
                FromPort = Port.MsSqlServer,
                ToPort = Port.MsSqlServer,
                IpProtocol = Protocol.Tcp
            });

            var deploy2TheCloudGroup = this.AddNew<AutoScalingGroup>();
            deploy2TheCloudGroup.VpcZoneIdentifier.Add(subnet1);
            deploy2TheCloudGroup.VpcZoneIdentifier.Add(subnet2);
            deploy2TheCloudGroup.TargetGroups.Add(targetGroup);

            var launchConfiguration = deploy2TheCloudGroup.AddNew<LaunchConfiguration>();
            launchConfiguration.AssociatePublicIpAddress = true;
            launchConfiguration.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;

            deploy2TheCloudGroup.LaunchConfiguration = launchConfiguration;
            launchConfiguration.BlockDeviceMappings.Add(new BlockDeviceMapping { Size = 50 });
            launchConfiguration.BlockDeviceMappings.Add(new BlockDeviceMapping { Size = 100, DeleteOnTermination = false });
            launchConfiguration.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("RdpGroup");

            launchConfiguration.Deployments.Add( new WebPlatformInstaller());

            WebPackage package = new WebPackage(deployPackageUriParameter, "LicenseServer");
            package.ParameterValues.Add("IIS Web Application Name", siteNameParameter);
            //<connectionStrings>
            //    <add name="AuthenticationConnectionString" connectionString="Data Source=is18dzcpeyazcql.cte6jojl9qjb.us-east-1.rds.amazonaws.com;Initial Catalog=Authorize;Persist Security Info=True;User ID='masterusername';Password='Ju83!#3.A*'" providerName="System.Data.OleDb" />

            package.ParameterValues.Add("AuthenticationConnectionString-Web.config Connection String",
                new JoinFunction(JoinFunction.DelimiterChar.None,
                "Initial Catalog=Authorize;Integrated Security=False;User Id=",
                sqlServer.MasterUsername,
                ";Password=",
                sqlServer.MasterUserPassword,
                ";MultipleActiveResultSets=True;Data Source="
                , sqlServer.GetEndPoint()));
            //
            deploy2TheCloudGroup.LaunchConfiguration.Deployments.Add("LicenseServerWebDeploy", package);

            var loadBalancer = this.AddNew<LoadBalancer>();
            var openToWorld = loadBalancer.SecurityGroups.AddNew<SecurityGroup>("SecurityGroupForLoadBalancer");
            openToWorld.SecurityGroupIngresses.Add(new Ingress()
            {
                CidrIp = "0.0.0.0/0",
                IpProtocol = Protocol.All,
                FromPort = Port.All,
                ToPort = Port.All
            });
            loadBalancer.Subnets.Add(subnet1);
            loadBalancer.Subnets.Add(subnet2);

            var httpsListener = this.AddNew<Listener>();
            httpsListener.LoadBalancer = loadBalancer;
            httpsListener.DefaultActions.Add(new DefaultAction()
            {
                TargetGroupArn = targetGroup,
                Type = DefaultAction.ActionType.Forward
            });
            httpsListener.Port = Port.Https;
            httpsListener.Protocol = ListenerProtocol.Https;
            httpsListener.Certificates.Add(new Listener.Certificate() { CertificateArn = certificateArnParameter });

            var dnsToLoadBalancer = this.AddNew<AliasTargetRecordSet>();
            dnsToLoadBalancer.HostedZoneName = hostedZoneName;
            dnsToLoadBalancer.RecordType = RecordSetType.A;
            dnsToLoadBalancer.AliasTarget.DnsName = new GetAttributeFunction(loadBalancer.Key, GetAttributeFunction.Attributes.LoadBalancerDnsName);
            dnsToLoadBalancer.AliasTarget.HostedZone = new GetAttributeFunction(loadBalancer.Key, GetAttributeFunction.Attributes.CanonicalHostedZoneIdV2);
            dnsToLoadBalancer.DependsOn.Add(package.WaitCondition);
        }
    }
}
