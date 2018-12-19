using System.Linq;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Outputs;
using YadaYada.Bisque.Aws.DirectoryService;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class MicrosoftAdTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            var vpc = this.Add(new Vpc("Vpc")).WithInternetAccess();
            vpc.EnableDnsHostnames = true;
            vpc.EnableDnsSupport = true;
            var directory = vpc.Add(new MicrosoftAd("MicrosoftAd1"));
            directory.AddSubnets();
            vpc.Resources.First(s => s.Value is Subnet).Value.Key = "Subnet4MicrosoftAd11";
            vpc.Resources.Last(s => s.Value is Subnet).Value.Key = "Subnet4MicrosoftAd12";

            var o = this.Outputs.AddNew<Output>();
            o.Value = directory.Name;
            o.Export.Name = new Substitute("${AWS::StackName}-DirectoryName");

            o = this.Outputs.AddNew<Output>();
            o.Value = directory.Password;
            o.Export.Name = new Substitute("${AWS::StackName}-DirectoryPassword");

            o = this.Outputs.AddNew<Output>();
            o.Value = new ReferenceFunction(vpc);
            o.Export.Name = new Substitute( "${AWS::StackName}-VpcId" );

            o = this.Outputs.AddNew<Output>();
            o.Value = new GetAttributeFunction(directory.Key,GetAttributeFunction.Attributes.DirectoryServiceSimpleAdAlias);
            o.Export.Name = new Substitute("${AWS::StackName}-DirectoryAlias");

            int i = 0;
            foreach (var vpcSubnet in vpc.Subnets)
            {
                i++;
                o = this.Outputs.AddNew<Output>();
                o.Value = new ReferenceFunction(vpcSubnet.RouteTable);
                o.Export.Name = new Substitute($"${{AWS::StackName}}-RouteTable{i}");

                o = this.Outputs.AddNew<Output>();
                o.Value = new ReferenceFunction(vpcSubnet);
                o.Export.Name = new Substitute($"${{AWS::StackName}}-Subnet{i}");
            }
        }
    }
}
