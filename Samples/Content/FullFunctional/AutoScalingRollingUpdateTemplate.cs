using YadaYada.Bisque.Aws.AutoScaling;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Google;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class AutoScalingRollingUpdateTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();

            var a = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewAutoScalingGroup();

            AutoScalingGroup.AutoScalingRollingUpdate rolling = new AutoScalingGroup.AutoScalingRollingUpdate()
            {
                WaitOnResourceSignals = true,
                MinInstancesInService = 1,
                PauseTime = "PT1H"

            };
            a.UpdatePolicy = rolling;
            
            var l = a.AddNew<LaunchConfiguration>();
            l.AssociatePublicIpAddress = true;
            l.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            l.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            l.Deployments.Add(new Chrome());


        }
    }
}
