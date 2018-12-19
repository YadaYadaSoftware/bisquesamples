using System;
using System.Reflection;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Instancing.Configuration.Deploy.Microsoft
{
    public class VisuallCppRuntimeTemplate : Template
    {
        protected override void InitializeTemplate()
        {

            var instance = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();

            var vcRt = instance.Deployments.Add(new VisualCppRuntime(ProcessorArchitecture.X86,
                VisualCppRuntime.VisualCppVersion.Version2008));

            vcRt.WaitCondition.Timeout = TimeSpan.FromMinutes(20);
        }
    }
}
