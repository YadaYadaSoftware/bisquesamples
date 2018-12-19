using System;
using System.Linq;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Conditions;
using YadaYada.Bisque.Aws.CloudFormation.Resources;
using YadaYada.Bisque.Aws.Common;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Build;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.EWSoftware;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Git;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Google;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Tfs;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.TypeMock;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.VisualStudio;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class TeamFoundationStackAmis : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<Vpc>()
                    .WithInternetAccess()
                    .WithNewSubnet()
                    .WithNewInstance()
                    .WithElasticIp();

            var condition = this.Add(new Condition(Resource.NormalizeKey("CreateSql"))
            {
                Value = new NotFunction(new EqualsFunction(i.InstanceType, ""))
            });

            i.Condition = condition;

            i.BlockDeviceMappings.RootDevice.Size = 60;
            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("RemoteDesktopSecurityGroupSql");
            var d =  i.Deployments.Add( new DisableFirewall());
            i.Deployments.Add( new DnsServerTools());
            var sql = i.Deployments.Add(new SqlServer()
            {
                Version = "2016",
                PrepareOnly = true
            });
            sql.WaitCondition.Timeout = new TimeSpan().MaxCloudFormationWait();
            var createAmi = i.Deployments.Add( new CreateAmi(i));
            createAmi.WaitCondition.Timeout = new TimeSpan().MaxCloudFormationWait();

            var v = this.Resources.Single(r => r.Value is Vpc).Value;
            //tfs
            i = v.AddNew<Instance>().WithElasticIp();
            condition = this.Add(new Condition(Resource.NormalizeKey("CreateTfs"))
            {
                Value = new NotFunction( new EqualsFunction(i.InstanceType, ""))
            });
            i.Condition = condition;

            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64; ;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("RemoteDesktopSecurityGroupTfs");
            i.Deployments.Add( new DnsServerTools());
            var tfs = i.Deployments.Add( new TeamFoundationServerApplicationTierInstall());
            tfs.AttributesFile.Content.Tfs.Version = "2017";
            createAmi = i.Deployments.Add( new CreateAmi(i));
            createAmi.WaitCondition.Timeout = new TimeSpan().MaxCloudFormationWait();

            //dev-build
            i = v.AddNew<Instance>().WithElasticIp();
            condition = this.Add(new Condition(Resource.NormalizeKey("CreateDevBuild"))
            {
                Value = new NotFunction( new EqualsFunction(i.InstanceType, ""))
            });
            i.Condition = condition;

            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;

            i.BlockDeviceMappings.RootDevice.Size = 50;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("RemoteDesktopSecurityGroupDev");
            i.Deployments.Add( new TfxCli());
            i.Deployments.Add( new DnsServerTools()); 
            i.Deployments.Add( new Chrome());
            i.Deployments.Add( new GitGui());
            i.Deployments.Add( new SandCastle());
            i.Deployments.Add( new Vs());
            i.Deployments.Add( new AwsCli());
            i.Deployments.AddNew<PowershellTools>();
            i.Deployments.Add( new TypeMockDeveloper());

            createAmi = i.Deployments.Add( new CreateAmi(i));
            createAmi.WaitCondition.Timeout = new TimeSpan().MaxCloudFormationWait();

            //enchilda
            i = v.AddNew<Instance>().WithElasticIp();
            condition = this.Add(new Condition(Resource.NormalizeKey("CreateEnchilda"))
            {
                Value = new NotFunction(new EqualsFunction(i.InstanceType, ""))
            });
            i.Condition = condition;

            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;

            i.BlockDeviceMappings.RootDevice.Size = 80;
            i.BlockDeviceMappings.RootDevice.Type = VolumeType.ProvisionedIops;
            i.BlockDeviceMappings.RootDevice.Ebs.IoPerSecond = i.BlockDeviceMappings.RootDevice.Size * 50;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("EnchildaRemoteDesktopSecurityGroup");

            i.Deployments.Add( new TypeMockServer());
            i.Deployments.Add( new TypeMockDeveloper());

            sql = i.Deployments.Add(new SqlServer()
            {
                Version = "2016",
                PrepareOnly = true
            });
            sql.WaitCondition.Timeout = new TimeSpan().MaxCloudFormationWait();
            tfs = i.Deployments.Add( new TeamFoundationServerApplicationTierInstall());
            tfs.AttributesFile.Content.Tfs.Version = "2017";
            i.Deployments.Add( new TfxCli());
            i.Deployments.Add( new DnsServerTools());
            i.Deployments.Add( new Chrome());
            i.Deployments.Add( new GitGui());
            i.Deployments.Add( new SandCastle());

            var vs = new Vs("2015");
            vs.WaitCondition.Timeout = new TimeSpan().MaxCloudFormationWait();
            i.Deployments.Add("EnchildaVs2015", vs);
            vs = new Vs("2017");
            vs.WaitCondition.Timeout = new TimeSpan().MaxCloudFormationWait();
            i.Deployments.Add("EnchildaVs2017", vs);
            i.Deployments.Add( new AwsCli());
            i.Deployments.AddNew<PowershellTools>();
            i.Deployments.Add(new CreateAmi(i));
        }
    }
}
