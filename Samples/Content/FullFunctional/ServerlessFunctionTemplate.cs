using System;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class ServerlessFunctionTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var v = this.AddNew<Vpc>();
            var function = this.AddNew<Function>();
            function.Properties.Handler = "AWSServerless4::AWSServerless4.Queue::NewFileUploaded";
            function.Properties.Runtime = Function.FunctionRuntime.DotNetCore10;
            function.Properties.Timeout = TimeSpan.FromMinutes(3);
            function.Properties.Environment.Variables.Add("QueueName", "SampleQueueName");
            function.VpcConfig.Subnets.Add(v.AddNew<Subnet>());
        }
    }
}