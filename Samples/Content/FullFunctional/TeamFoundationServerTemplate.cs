using System;
using System.Linq;
using YadaYada.Bisque.Aws.AutoScaling;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Conditions;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.CloudFormation.Parameters.Psuedo;
using YadaYada.Bisque.Aws.CloudFormation.Resources;
using YadaYada.Bisque.Aws.DirectoryService;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Build;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.EWSoftware;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Git;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Google;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Tfs;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows.ActiveDirectory;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows.Share;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.NodeDotOrg;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Redgate;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.TypeMock;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.VisualStudio;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Iam;
using YadaYada.Bisque.Aws.Iam.Roles;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class TeamFoundationServerTemplate : Template
    {
        const string TfsKey = "Tfs";
        const string SqlKey = "Sql4Tfs";
        const string ZeroServerText = "No Servers (Directory Only)";
        const string OneServerText = "Single Server (SQL Server & TFS & Build Agent)";
        const string TwoServerText = "Dual Servers (SQL Server & TFS/Build Agent)";
        const string ThreeServerText = "Three Servers (SQL Server/TFS/Build Agent)";
        const string OneOrMoreServersKey = "OneOrMoreServers";
        const string OneOrTwoServersKey = "OneOrTwoServers";
        const string TwoOrThreeServers = "TwoOrThreeServers";

        protected override void InitializeTemplate()
        {
            Parameter sizeParameter = new Parameter("Size")
            {
                GroupLabel = "General Settings",
                Label = "Stack Size",
                // ReSharper disable once UseStringInterpolation
                Description = string.Format("Choose the size of the stack.  If your requirements dictate that accounts need to be created before Team Foundation Server installation and configuration, choose '{0}'", ZeroServerText ),
                Default = ThreeServerText
            };

            this.Add(sizeParameter);

            sizeParameter.Type = ParameterType.String;
            sizeParameter.AllowedValues.Add(ZeroServerText);
            sizeParameter.AllowedValues.Add(OneServerText);
            sizeParameter.AllowedValues.Add(TwoServerText);
            sizeParameter.AllowedValues.Add(ThreeServerText);

            var oneServerCondition =
                this.Add(new Condition(Resource.NormalizeKey(OneServerText))
                {
                    Value = new EqualsFunction(new ReferenceFunction(sizeParameter), OneServerText)
                });

            var twoServerCondition =
                this.Add(new Condition(Resource.NormalizeKey(TwoServerText))
                {
                    Value = new EqualsFunction(new ReferenceFunction(sizeParameter), TwoServerText)
                });

            var threeServerCondition = this.Add(new Condition(Resource.NormalizeKey(ThreeServerText))
            {
                Value = new EqualsFunction(new ReferenceFunction(sizeParameter), ThreeServerText)
            });

            var oneOrTwoServersCondition = this.Add(new Condition(Resource.NormalizeKey(OneOrTwoServersKey))
            {
                Value = new OrFunction(oneServerCondition.Value, twoServerCondition.Value)
            });

            var oneOrMoreServersCondition = this.Add(new Condition(Resource.NormalizeKey(OneOrMoreServersKey))
            {
                Value = new OrFunction(oneServerCondition.Value, twoServerCondition.Value, threeServerCondition.Value)
            });

            var twoOrThreeServersCondition = this.Add(new Condition(Resource.NormalizeKey(TwoOrThreeServers))
            {
                Value = new OrFunction(twoServerCondition.Value, threeServerCondition.Value)
            });

            // create the vpc
            Vpc v = this.AddNew<Vpc>();
            v.WithInternetAccess();

            // create a Microsoft AD
            MicrosoftAd activeDirectory = v.AddNew<MicrosoftAd>();
            activeDirectory.AddSubnets();
            v.Subnets.ToList()[0].AvailabilityZone = new SelectFunction(0, new AvailabilityZonesFunction());
            v.Subnets.ToList()[1].AvailabilityZone = new SelectFunction(1, new AvailabilityZonesFunction());

            // create the sql server instance
            Instance sql = v.Subnets.First().AddNew<Instance>();
            sql.Key = SqlKey;
            sql.WithElasticIp();
            sql.Condition = oneOrMoreServersCondition;

            sql.Deployments.Add(new DisableFirewall());
            activeDirectory.AddToDomain(sql);
            // allow rdp access
            sql.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("SecurityGroupForRdpToSqlServer");

            sql.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            sql.RootDevice.Size = 100;
            sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 25);
            sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 25);
            sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 5);

            // install sql server
            SqlServer sqlServerApplicationDeployment = sql.Deployments.Add( new SqlServer() { Condition= oneOrMoreServersCondition});
            sqlServerApplicationDeployment.InstallFromPrepared = true;
            sqlServerApplicationDeployment.AttributesFile.Content.SqlServer.DataDirectory = "d:\\sql";
            sqlServerApplicationDeployment.AttributesFile.Content.SqlServer.LogDirectory = "e:\\log";
            sqlServerApplicationDeployment.AttributesFile.Content.SqlServer.BackupDirectory = "f:\\backup";
            sql.ImageIdDeploymentDemands.Add(sqlServerApplicationDeployment.DemandName);


            // clone the tfs application tier installation from the clonable instance
            var tfsDeploymentOnSql = AddTfsApplicationTier(sql, activeDirectory, oneOrTwoServersCondition, sql, sqlServerApplicationDeployment);

            // clone the build server application installation
            TeamFoundationBuild buildOnSql = AddTfsBuildTier(sql, tfsDeploymentOnSql, oneServerCondition,activeDirectory, sql);

            // create the TFS application server 
            Instance tfsInstance = v.Subnets.First().AddNew<Instance>();
            tfsInstance.Key = TfsKey;
            tfsInstance.WithElasticIp();
            tfsInstance.Condition = threeServerCondition;


            tfsInstance.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            tfsInstance.RootDevice.Size = 100;
            tfsInstance.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("SecurityGroupForRdpToTfsServer");

            activeDirectory.AddToDomain(tfsInstance);

            TeamFoundationServerApplicationTierConfigure tfsDeploymentOnTfs = AddTfsApplicationTier(tfsInstance, activeDirectory,
                threeServerCondition, sql, sqlServerApplicationDeployment);


            // create the build server group
            AutoScalingGroup asg = v.AddNew<AutoScalingGroup>();

            var update = new AutoScalingGroup.AutoScalingRollingUpdate();
            update.WaitOnResourceSignals = true;
            asg.UpdatePolicy = update;
            update.MinInstancesInService = 1;
            update.MaxBatchSize = 1;
            update.PauseTime = "PT1H";


            foreach (Subnet vSubnet in v.Subnets)
            {
                asg.VpcZoneIdentifier.Add(vSubnet);
            }


            asg.Condition = twoOrThreeServersCondition;

            asg.LaunchConfiguration = asg.AddNew<LaunchConfiguration>();
            asg.Key = "BuildServer";
            LaunchConfiguration buildConfiguration = asg.LaunchConfiguration;
            buildConfiguration.AssociatePublicIpAddress = true;
            buildConfiguration.AlwaysReplace = true;
            activeDirectory.AddToDomain(buildConfiguration);

            buildConfiguration.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("SecurityGroupForRdpToBuild");
            buildConfiguration.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            buildConfiguration.RootDevice.Size = 100;
            var share = buildConfiguration.Deployments.Add(
                new WindowsShare("backup", "c://backup", v.VpcCidrBlock));


            var buildDeploymentAgainstBuildGroup = AddTfsBuildTier(
                buildConfiguration,
                tfsDeploymentOnTfs,
                twoOrThreeServersCondition,
                activeDirectory,
                tfsInstance);
            // give access to the build server (on build) to the two subnets
            buildDeploymentAgainstBuildGroup.Connect(activeDirectory.VpcSettings.Subnets.First(), activeDirectory.VpcSettings.Subnets.Last());

            AutoScalingGroup developerGroup = v.AddNew<AutoScalingGroup>();
            developerGroup.Condition = oneOrMoreServersCondition;
            foreach (Subnet vSubnet in v.Subnets)
            {
                developerGroup.VpcZoneIdentifier.Add(vSubnet);
            }

            developerGroup.LaunchConfiguration = developerGroup.AddNew<LaunchConfiguration>();

            //"DeveloperConfiguration",
            //developerGroup.Options = LaunchConfigurationOptions.Windows 
            //    | LaunchConfigurationOptions.AssociatePublicIpAddress;
            LaunchConfiguration developerConfiguration = developerGroup.LaunchConfiguration;
            AddCloudFormationPolicy(developerConfiguration);
            developerConfiguration.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("SecurityGroupForRdpToDeveloper");
            developerConfiguration.RootDevice.Size = 100;
            developerConfiguration.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;

            developerConfiguration.Deployments.Add( new Chrome());
            var awsCli =  developerConfiguration.Deployments.Add(new AwsCli());
            developerConfiguration.Deployments.Add( new SandCastle());
            developerConfiguration.Deployments.Add( new GitGui());
            developerConfiguration.Deployments.Add( new DotNetFrameworkSdk());
            developerConfiguration.Deployments.Add( new SmartAssembly());
            developerConfiguration.Deployments.Add( new TypeMockDeveloper());
            developerConfiguration.Deployments.Add( new Node());
            developerConfiguration.Deployments.Add( new TfxCli());
            developerConfiguration.Deployments.Add( new Vs());
            developerConfiguration.Deployments.AddNew<ReSharper>();
            activeDirectory.AddToDomain(developerConfiguration);


            //open sqlserver port up to the subnets
            sqlServerApplicationDeployment.Connect(activeDirectory.VpcSettings.Subnets.First(), activeDirectory.VpcSettings.Subnets.Last());
            // give access to the TFS application server (on Tfs) to the two subnets
            tfsDeploymentOnTfs.Connect(activeDirectory.VpcSettings.Subnets.First(), activeDirectory.VpcSettings.Subnets.Last());
            // give access to the TFS application server (on Sql) to the two subnets
            tfsDeploymentOnSql.Connect(activeDirectory.VpcSettings.Subnets.First(), activeDirectory.VpcSettings.Subnets.Last());
        }

        private static TeamFoundationServerApplicationTierConfigure AddTfsApplicationTier(
            Instance addTfsApplicationTierTo, 
            MicrosoftAd activeDirectory, 
            Condition conditionForApplicationTier, 
            Instance sqlInstance, 
            SqlServer sqlServerApplicationDeployment)
        {

            var waitForSql = new WaitFor(sqlServerApplicationDeployment.WaitCondition)
            {
                Key = $"WaitForSqlDeploymentOn{addTfsApplicationTierTo.Key}"
            };
            addTfsApplicationTierTo.Deployments.Add(waitForSql);
            waitForSql.Condition = conditionForApplicationTier;

            TeamFoundationServerApplicationTierConfigure tfsApplicationTier = new TeamFoundationServerApplicationTierConfigure
            {
                Key = $"TfsApplicationTierOn{addTfsApplicationTierTo.Key}",
                Condition = conditionForApplicationTier
            };

            tfsApplicationTier.AttributesFile.Content.Tfs.SqlInstanceEndPoint = sqlInstance.Fqdn;
            tfsApplicationTier.AttributesFile.Content.Tfs.ApplicationTierFqdn = addTfsApplicationTierTo.Fqdn;
            tfsApplicationTier.AttributesFile.Content.Tfs.UserName = activeDirectory.AdminUser;
            tfsApplicationTier.AttributesFile.Content.Tfs.Password = activeDirectory.Password;
            tfsApplicationTier.AttributesFile.Content.Tfs.DefaultCollectionName = string.Empty;



            // add it to the instance
            addTfsApplicationTierTo.Deployments.Add(tfsApplicationTier);
            addTfsApplicationTierTo.ImageIdDeploymentDemands.Add(tfsApplicationTier.DemandName);

            tfsApplicationTier.WaitCondition.Timeout = TimeSpan.FromHours(6);

            return tfsApplicationTier;
        }

        private static TeamFoundationBuild AddTfsBuildTier(
            ILaunchConfiguration configurationToAddBuildTo,
            TeamFoundationServerApplicationTierConfigure tfsDeployment, 
            Condition conditionForBuildDeployments, 
            MicrosoftAd activeDirectory, 
            Instance instanceRunningTfs)
        {
            configurationToAddBuildTo.Deployments.Add( new AwsCli() {Condition = conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new TypeMockServer() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new TypeMockDeveloper() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new Node() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new TfxCli() {Condition = conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new SandCastle() {Condition = conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new DotNetFrameworkSdk() {Condition = conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new GitGui() {Condition = conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new Vs() {Condition = conditionForBuildDeployments});

            var addNetworkServiceToAdminGroup = configurationToAddBuildTo.Deployments.Add(
                new AddDirectoryUserToLocalGroup("\"NETWORK SERVICE\"", "Administrators")
                {
                    Condition = conditionForBuildDeployments
                });

            WaitFor waitForTfs = new WaitFor(tfsDeployment.WaitCondition);
            waitForTfs.Key = $"WaitForTfsApplicationTierOn{configurationToAddBuildTo.Key}";
            waitForTfs.Condition = conditionForBuildDeployments;
            configurationToAddBuildTo.Deployments.Add(waitForTfs);

            var install = configurationToAddBuildTo.Deployments.Add(
                new InstallTfxTask(
                    new JoinFunction(JoinFunction.DelimiterChar.None, "http://", instanceRunningTfs.Fqdn, ":8080/tfs"),
                    "http://deploy2the.cloud/nuget/nuget",
                    "Bisque.Tfs",
                    activeDirectory.AdminUser,
                    activeDirectory.Password)
                    { Condition = conditionForBuildDeployments });

            
            TeamFoundationBuild buildDeployment = new TeamFoundationBuild()
            {
                Condition = conditionForBuildDeployments
            };

            buildDeployment.AttributesFile.Content.Tfs.UserName = activeDirectory.AdminUser;
            buildDeployment.AttributesFile.Content.Tfs.Password = activeDirectory.Password;
            buildDeployment.AttributesFile.Content.Tfs.DefaultCollectionName = string.Empty;
            buildDeployment.AttributesFile.Content.Tfs.ApplicationTierFqdn = instanceRunningTfs.Fqdn;


            configurationToAddBuildTo.Deployments.Add($"BuildDeploymentOn{configurationToAddBuildTo.Key}", buildDeployment);

            AddCloudFormationPolicy(configurationToAddBuildTo);

            buildDeployment.WaitCondition.Timeout = TimeSpan.FromHours(6);

            return buildDeployment;
        }

        private static void AddCloudFormationPolicy(ILaunchConfiguration configurationToAddBuildTo)
        {
            LaunchConfiguration l = configurationToAddBuildTo as LaunchConfiguration;
            Instance i = configurationToAddBuildTo as Instance;
            var policy = new CloudFormationPolicyProperties();

            if (l != null)
            {
                policy.PolicyName = $"CloudFormationPolicyFor{l.Key}";
                if (l.InstanceProfile==null) l.InstanceProfile = l.AddNew<InstanceProfile>();
                if (l.InstanceProfile.Roles.Count == 0) l.AddNew<Role>();
                l.InstanceProfile.Roles.First().Policies.Add(policy);
            }
            else if (i != null)
            {
                policy.PolicyName = $"CloudFormationPolicyFor{i.Key}";
                i.InstanceProfile.Roles.First().Policies.Add(policy);
            }
        }
    }
}
