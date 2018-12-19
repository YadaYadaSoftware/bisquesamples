using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.Lambda
{
    public class CreateAmiTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            // create a new Vpc
            var v = this.AddNew<Vpc>();
            // add an InternetGateway and attach it to the Vpc
            v.WithInternetAccess();
            // create a new Subnet and attach it to the Vpc
            var subnet = v.AddNew<Subnet>();
            // create a route to the InternetGateway
            subnet.WithInternetAccessVia<InternetGateway>();
            // add a new Instance to the Subnet
            var i = subnet.AddNew<Instance>();
            // add a new ElasticIp
            i.WithElasticIp();
            // create a new SecurityGroup allowing access via RDP
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            i.Tags.Add("NewTag",new Tag("NewTag","MyValue"));
            // create an Ami Image from the instance
            var createAmi = new CreateAmi(i);
            i.Deployments.Add(createAmi);
            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
        }
    }
}
