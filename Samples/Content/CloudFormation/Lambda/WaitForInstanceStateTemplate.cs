using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Outputs;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.Lambda
{
    public class WaitForInstanceStateTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            
            var v = this.AddNew<Vpc>();
            v.WithInternetAccess();
            var subnet = v.AddNew<Subnet>();
            subnet.WithInternetAccessVia<InternetGateway>();
            var i = subnet.AddNew<Instance>();
            i.WithElasticIp();
            i.ImageId = "ami-ee7805f9";
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            var sysprep = i.Deployments.Add(new SysPrep());

            var f = this.AddNew<WaitOnState>();
            f.DependsOn.Add(sysprep.WaitCondition);
            var c = this.AddNew<WaitOnState.WaitOnStateCustom>();

            c.ServiceToken = new GetAttributeFunction(f.Key, GetAttributeFunction.Attributes.Arn);
            c.Properties.Instance = i;

            var o = new Output("InstanceState")
            {
                Value = new GetAttributeFunction(c.Key, "InstanceState")
            };
            this.Outputs.Add(o);
        }
    }
}
