using System;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.WordpressDotOrg;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Instancing.Configuration.Deploy.WordPressDotOrg
{
    public class WordPressTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            var instance = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();

            instance.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            //instance.Deployments.Add( new Iis>();
            var wordPress = instance.Deployments.Add(new WordPress());
            wordPress.WaitCondition.Timeout = TimeSpan.FromHours(1);


        }
    }
}
