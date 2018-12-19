using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SimpleCreateAmi : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var v = this.AddNew<Vpc>();
            v.VpcCidrBlock = "10.0.0.0/16";
            var s = v.WithNewSubnet();
            s.CidrBlock = "10.0.0.0/24";
            var i = s.WithNewInstance();
            i.InstanceType = InstanceType.T2Nano;
            i.WithElasticIp();

            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;

            i.BlockDeviceMappings.RootDevice.Size = 50;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            i.Deployments.Add(new CreateAmi(i));
        }
    }
}
