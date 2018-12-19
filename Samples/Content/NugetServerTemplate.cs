using System.Linq;
using System.Reflection.Metadata;
using YadaYada.Bisque.Aws.AutoScaling;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.CloudFormation.Parameters.Psuedo;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.ElasticLoadBalancingV2;
using YadaYada.Bisque.Aws.Route53;
using Parameter = YadaYada.Bisque.Aws.CloudFormation.Parameters.Parameter;

namespace YadaYada.Bisque.Aws.Samples.Content
{
    public class NugetServerTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            var certicateArn = this.Add(new CertificateArnParameter("LoadBalancerCertificateArn", "Load Balancer Certificate"));

            var nugetServerGroup = this.AddNew<Vpc>().WithInternetAccess()
                .WithNewSubnet()
                .WithNewAutoScalingGroup();

            Vpc vpc = (Vpc) this.Resources.Single(r => r.Value is Vpc).Value;

            var subnet1 = vpc.AddNew<Subnet>().WithInternetAccessVia<InternetGateway>();
            subnet1.AvailabilityZone = new SelectFunction(0, new AvailabilityZonesFunction());

            var subnet2 = vpc.AddNew<Subnet>().WithInternetAccessVia<InternetGateway>();
            subnet2.AvailabilityZone = new SelectFunction(1, new AvailabilityZonesFunction());
            var nugetTargetGroup = vpc.AddNew<TargetGroup>();
            nugetTargetGroup.Protocol = TargetGroupProtocol.Http;
            nugetTargetGroup.Port = Port.Http;

            //var nugetServerGroup = vpc.AddNew<AutoScalingGroup>();
            nugetServerGroup.VpcZoneIdentifier.Add(subnet1);
            nugetServerGroup.VpcZoneIdentifier.Add(subnet2);
            nugetServerGroup.TargetGroups.Add(nugetTargetGroup);

            nugetServerGroup.LaunchConfiguration = nugetServerGroup.AddNew<LaunchConfiguration>();
            nugetServerGroup.LaunchConfiguration.AssociatePublicIpAddress = true;

            nugetServerGroup.LaunchConfiguration.BlockDeviceMappings.Add(new BlockDeviceMapping { Size = 50 });
            nugetServerGroup.LaunchConfiguration.BlockDeviceMappings.Add(new BlockDeviceMapping { Size = 100, DeleteOnTermination = false });

            nugetServerGroup.LaunchConfiguration.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();

            WebPackage nugetWeb = new WebPackage("https://s3.amazonaws.com/yadayada/NugetServer", "NugetServer");
            var apiKeyParameter = this.Add(new Parameter("NuGetServerApiKey")
            {
                Type = ParameterType.String,
                Label = "ApiKey",
                GroupLabel = "NuGet Server",
                NoEcho = true,
                ConstraintDescription = "Must be at least 4 in length."
            });
            //nugetWebDeploy.ParameterValues.Add("ApiKey", apiKeyParameter);

            var nugetPackagesPathParameter = this.Add(new Parameter("NugetServerPackagesPath")
            {
                Type = ParameterType.String,
                Label = "Packages Path",
                GroupLabel = "NuGet Server"
            });
            //nugetWebDeploy.ParameterValues.Add("PackagesPath", nugetPackagesPathParameter);

            nugetServerGroup.LaunchConfiguration.Deployments.Add(nugetWeb);

            var openToWorld = vpc.AddNew<SecurityGroup>();
            openToWorld.SecurityGroupIngresses.Add(new Ingress()
            {
                CidrIp = "0.0.0.0/0",
                IpProtocol = Protocol.All,
                FromPort = Port.All,
                ToPort = Port.All
            });

            var nugetLoadBalancer = vpc.AddNew<LoadBalancer>();
            nugetLoadBalancer.SecurityGroups.Add(openToWorld);
            nugetLoadBalancer.Subnets.Add(subnet1);
            nugetLoadBalancer.Subnets.Add(subnet2);

            var nugetHttpsListener = vpc.AddNew<Listener>();
            nugetHttpsListener.LoadBalancer = nugetLoadBalancer;
            nugetHttpsListener.DefaultActions.Add(new DefaultAction()
            {
                TargetGroupArn = nugetTargetGroup,
                Type = DefaultAction.ActionType.Forward
            });
            nugetHttpsListener.Port = Port.Https;
            nugetHttpsListener.Protocol = ListenerProtocol.Https;

            var nugetHttpListener = vpc.AddNew<Listener>();
            nugetHttpListener.LoadBalancer = nugetLoadBalancer;
            nugetHttpListener.DefaultActions.Add(new DefaultAction()
            {
                TargetGroupArn = nugetTargetGroup,
                Type = DefaultAction.ActionType.Forward
            });
            nugetHttpListener.Port = Port.Http;
            nugetHttpListener.Protocol = ListenerProtocol.Http;

            nugetHttpsListener.Certificates.Add(new Listener.Certificate() { CertificateArn = certicateArn });

            var nugetDnsToLoadBalancer = this.AddNew<AliasTargetRecordSet>();

            nugetDnsToLoadBalancer.RecordType = RecordSetType.A;
            nugetDnsToLoadBalancer.AliasTarget.DnsName = new GetAttributeFunction(nugetLoadBalancer.Key, GetAttributeFunction.Attributes.LoadBalancerDnsName);
            nugetDnsToLoadBalancer.AliasTarget.HostedZone = new GetAttributeFunction(nugetLoadBalancer.Key, GetAttributeFunction.Attributes.CanonicalHostedZoneNameId);
        }
    }
}
