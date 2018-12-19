using System;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.iCSharpCode;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.InternetExplorer;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content
{
    public class InstallBisqueTemplate : Template
    {

        protected override void InitializeTemplate()
        {
            var instance = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();

            instance.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();

            var p = this.Add(new Parameter("BisqueVersion")
            {
                Type = ParameterType.String
            });

            instance.Deployments.Add(new InstallBisque(p));

            CreateStartupShortcut shortcut = new CreateStartupShortcut("explorer.exe",
                "WelcomeAwsMarketplaceBisqueUser.lnk",
                "http://deploy2the.cloud/help");
            instance.Deployments.Add(shortcut);
            instance.Deployments.Add(new AddTrustedSite());
            instance.Deployments.Add(new SharpDevelop());
            var createAmi = instance.Deployments.Add(new CreateAmi(instance));
            createAmi.WaitCondition.Timeout = TimeSpan.FromHours(1);
        }
    }
}
