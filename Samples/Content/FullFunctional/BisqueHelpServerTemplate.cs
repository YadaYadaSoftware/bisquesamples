using System.Linq;
using YadaYada.Bisque.Aws.AutoScaling;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.ElasticLoadBalancingV2;
using YadaYada.Bisque.Aws.Route53;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class BisqueHelpServerTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            var certicateArn = this.Add(new CertificateArnParameter("LoadBalancerCertificateArn", "Load Balancer Certificate"));

            var vpc = this.AddNew<Vpc>().WithInternetAccess();

            var targetGroup = vpc.AddNew<TargetGroup>();
            targetGroup.Protocol = TargetGroupProtocol.Http;
            targetGroup.Port = Port.Http;

            var group = vpc.WithNewAutoScalingGroup();
            group.TargetGroups.Add(targetGroup);

            group.LaunchConfiguration = group.AddNew<LaunchConfiguration>();
            group.LaunchConfiguration.AssociatePublicIpAddress=true;

            group.LaunchConfiguration.BlockDeviceMappings.Add(new BlockDeviceMapping { Size = 50 });
            group.LaunchConfiguration.BlockDeviceMappings.Add(new BlockDeviceMapping { Size = 100, DeleteOnTermination = false });

            group.LaunchConfiguration.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();

            var p = this.Add(new Parameter("BisqueHelpServerUri"));
            p.Type = ParameterType.String;
            p.Label = "Uri to root of package";


            WebPackage BisqueHelp = new WebPackage(p, "Doc.Web");
            BisqueHelp.ParameterValues.Add("IIS Web Application Name","BisqueAwsHelp");

            group.LaunchConfiguration.Deployments.Add(BisqueHelp);

            var openToWorld = vpc.AddNew<SecurityGroup>();
            openToWorld.SecurityGroupIngresses.Add(new Ingress()
            {
                CidrIp = "0.0.0.0/0",
                IpProtocol = Protocol.All,
                FromPort = Port.All,
                ToPort = Port.All
            });

            var loadBalancer = vpc.AddNew<LoadBalancer>();
            loadBalancer.SecurityGroups.Add(openToWorld);
            loadBalancer.Subnets.Add(vpc.Subnets.First());
            loadBalancer.Subnets.Add(vpc.Subnets.Last());

            var httpsListener = vpc.AddNew<Listener>();
            httpsListener.LoadBalancer = loadBalancer;
            httpsListener.DefaultActions.Add(new DefaultAction()
            {
                TargetGroupArn = targetGroup,
                Type = DefaultAction.ActionType.Forward
            });
            httpsListener.Port = Port.Https;
            httpsListener.Protocol = ListenerProtocol.Https;

            var httpListener = vpc.AddNew<Listener>();
            httpListener.LoadBalancer = loadBalancer;
            httpListener.DefaultActions.Add(new DefaultAction()
            {
                TargetGroupArn = targetGroup,
                Type = DefaultAction.ActionType.Forward
            });
            httpListener.Port = Port.Http;
            httpListener.Protocol = ListenerProtocol.Http;

            httpsListener.Certificates.Add(new Listener.Certificate() { CertificateArn = certicateArn });

            var dnsToLoadBalancer = this.AddNew<AliasTargetRecordSet>();
            dnsToLoadBalancer.RecordType = RecordSetType.A;
            dnsToLoadBalancer.AliasTarget.DnsName = new GetAttributeFunction(loadBalancer.Key, GetAttributeFunction.Attributes.LoadBalancerDnsName);
            dnsToLoadBalancer.AliasTarget.HostedZone = new GetAttributeFunction(loadBalancer.Key, GetAttributeFunction.Attributes.CanonicalHostedZoneNameId);
        }
    }
}
