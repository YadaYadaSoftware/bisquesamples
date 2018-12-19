using System;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.Common;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.VisualStudio;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class VisualStudioTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();

            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;

            i.BlockDeviceMappings.RootDevice.Size = 50;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            i.Deployments.Add( new Vs());

            var createAmi = i.Deployments.Add(new CreateAmi(i));
            createAmi.WaitCondition.Timeout = new TimeSpan().MaxCloudFormationWait();
        }

    }
}
