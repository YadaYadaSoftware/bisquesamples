using System;
using System.Linq;
using YadaYada.Bisque.Aws.AutoScaling;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Conditions;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Metadata;
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
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Directory;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Tfs;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows.ActiveDirectory;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows.Share;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.NodeDotOrg;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.TypeMock;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.VisualStudio;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Iam;
using YadaYada.Bisque.Aws.Iam.Roles;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class TeamFoundationServerManualChoices : Template
    {
        const string TfsKey = "Tfs";
        const string SqlKey = "Sql4Tfs";
        private const string CreateServerOneConditionKey = "CreateServerOne";
        private const string CreateServerTwoConditionKey = "CreateServerTwo";
        private const string CreateBuildLaunchConfigurationConditionKey = "CreateBuildLaunchConfiguration";
        private const string CreateDeveloperLaunchConfigurationConditionKey = "CreateDeveloperLaunchConfiguration";
        private const string InstallTfsOnSqlServerConditionKey = "InstallTfsOnSqlServer";
        private const string InstallBuildOnSqlServerConditionKey = "InstallBuildOnSqlServer";
        private const string InstallBuildOnTfsConditionKey = "InstallBuildOnTfs";
        private const string TfsApplicationWaitConditionKey = "TfsApplicationTier";
        protected override void InitializeTemplate()
        {
            var tfsVersionParameter = this.Add(new StringParameter("TfsVersion")) as StringParameter;
            tfsVersionParameter.AllowedValues.Add("2015");
            tfsVersionParameter.AllowedValues.Add("2017");

            WaitCondition waitConditionTfsApplicationTier = this.AddNew<WaitCondition>();
            // create the vpc
            Vpc v = this.AddNew<Vpc>().WithInternetAccess();

            // create a Microsoft AD
            MicrosoftAd activeDirectory = v.WithMicrosoftAd();
            v.Subnets.ToList()[0].AvailabilityZone = new SelectFunction(0, new AvailabilityZonesFunction());
            v.Subnets.ToList()[1].AvailabilityZone = new SelectFunction(1, new AvailabilityZonesFunction());

            // create the sql server instance
            Instance sql = activeDirectory.WithNewInstance().WithElasticIp();
            Instance tfsInstance = activeDirectory.WithNewInstance().WithElasticIp();

            var sqlServerInstanceCondition =
                this.Add(new Condition(Resource.NormalizeKey(CreateServerOneConditionKey))
                {
                    Value = new NotFunction(new EqualsFunction(new ReferenceFunction((CloudVariant)sql.InstanceType), string.Empty))
                });

            // create the TFS application server 
            var tfsServerInstanceCondition =
                this.Add(new Condition(Resource.NormalizeKey(CreateServerTwoConditionKey))
                {
                    Value = new NotFunction(new EqualsFunction(new ReferenceFunction((CloudVariant)tfsInstance.InstanceType), string.Empty))
                });

            waitConditionTfsApplicationTier.Condition = this.Add(new Condition(Resource.NormalizeKey("TfsApplicationTierWaitConditionCondition"))
            {
                Value = new OrFunction(sqlServerInstanceCondition.Value, tfsServerInstanceCondition.Value)
            });

           

            // create the build server group
            AutoScalingGroup asg = v.AddNew<AutoScalingGroup>();
            asg.Key = "Asg";
            var buildServerCondition = this.Add(new Condition(Resource.NormalizeKey(CreateBuildLaunchConfigurationConditionKey))
            {
                Value = new NotFunction(new EqualsFunction(new ReferenceFunction(asg.MinSize), "0"))
            });
            asg.Condition = buildServerCondition;

            sql.Condition = sqlServerInstanceCondition;

            sql.Deployments.Add(new DisableFirewall());
            activeDirectory.AddToDomain(sql);
            var renameSqlToTfs = this.Add(new Condition("RenameSqlToTfsCondition")
            {
                Value = new AndFunction(sqlServerInstanceCondition.Value, new NotFunction(tfsServerInstanceCondition))
            });

            var renameToTfs = new AddCnameRecordToDirectory("Tfs", activeDirectory.Name, "admin", activeDirectory.Password);
            renameToTfs.Condition = renameSqlToTfs;
            sql.Deployments.Add($"{renameToTfs.Key}{sql.Key}", renameToTfs);
            

            // allow rdp access
            sql.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("SecurityGroupForRdpToSqlServer");

            sql.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            sql.RootDevice.Size = 100;
            sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 25);
            sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 25);
            sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 5);

            // install sql server
            SqlServer sqlServerApplicationDeployment = sql.Deployments.Add( new SqlServer());
            sqlServerApplicationDeployment.InstallFromPrepared = true;
            sqlServerApplicationDeployment.AttributesFile.Content.SqlServer.DataDirectory = "d:\\sql";
            sqlServerApplicationDeployment.AttributesFile.Content.SqlServer.LogDirectory = "e:\\log";
            sqlServerApplicationDeployment.AttributesFile.Content.SqlServer.BackupDirectory = "f:\\backup";
            sql.ImageIdDeploymentDemands.Add(sqlServerApplicationDeployment.DemandName);

            var conditionApplicationTierOnSql =
                this.Add(new Condition(Resource.NormalizeKey(InstallTfsOnSqlServerConditionKey))
                {
                    Value = new AndFunction(sqlServerInstanceCondition.Value, new NotFunction(tfsServerInstanceCondition))
                });

            var forceBuildOnSql = this.Add(new StringParameter("InstallBuildOnSql"));
            ((Interface)this.Metadata[Interface.InterfaceKey]).ParameterGroups.SingleOrDefault(
                p => p.Label == sql.Parameters.First().Value.GroupLabel).Parameters.Add(forceBuildOnSql.Key);
            forceBuildOnSql.Label = "Install Build Agent on SQL";
            forceBuildOnSql.Description = "Enter any non-zero length string to install build agent on SQL.";

            var forceBuildOnTfs = this.Add(new StringParameter("InstallBuildOnTfs"));
            ((Interface)this.Metadata[Interface.InterfaceKey]).ParameterGroups.SingleOrDefault(
                p => p.Label == tfsInstance.Parameters.First().Value.GroupLabel).Parameters.Add(forceBuildOnTfs.Key);
            forceBuildOnTfs.Label = "Install Build Agent on TFS";
            forceBuildOnTfs.Description = "Enter any non-zero length string to install build agent on TFS.";

            var conditionBuildOnSql =
                this.Add(new Condition(Resource.NormalizeKey(InstallBuildOnSqlServerConditionKey))
                {
                    Value = new OrFunction(new NotFunction(new EqualsFunction(forceBuildOnSql,string.Empty)) ,new AndFunction(sqlServerInstanceCondition.Value, new NotFunction(tfsServerInstanceCondition), new NotFunction(buildServerCondition)))
                });

            var conditionBuildOnTfs =
                this.Add(new Condition(Resource.NormalizeKey(InstallBuildOnTfsConditionKey))
                {
                    Value = new OrFunction(new NotFunction(new EqualsFunction(forceBuildOnTfs, string.Empty)), new AndFunction(tfsServerInstanceCondition.Value, new NotFunction(sqlServerInstanceCondition), new NotFunction(buildServerCondition)))
                });

            // clone the tfs application tier installation from the clonable instance
            var applicationTier = AddTfsApplicationTier(sql, activeDirectory, conditionApplicationTierOnSql, sql, sqlServerApplicationDeployment, waitConditionTfsApplicationTier, tfsVersionParameter);
            
            tfsInstance.Condition = tfsServerInstanceCondition;

            tfsInstance.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            tfsInstance.RootDevice.Size = 100;
            tfsInstance.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("SecurityGroupForRdpToTfsServer");

            activeDirectory.AddToDomain(tfsInstance);

            TeamFoundationServerApplicationTierConfigure tfsDeploymentOnTfs = AddTfsApplicationTier(tfsInstance, activeDirectory,
                null, sql, sqlServerApplicationDeployment, waitConditionTfsApplicationTier, tfsVersionParameter);



            TeamFoundationBuild buildOnSql = AddTfsBuildTier(sql, applicationTier, conditionBuildOnSql, activeDirectory, sql);
            TeamFoundationBuild buildOnTfs = AddTfsBuildTier(tfsInstance, applicationTier, conditionBuildOnTfs, activeDirectory, sql);


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


            asg.LaunchConfiguration = asg.AddNew<LaunchConfiguration>();
            asg.LaunchConfiguration.AssociatePublicIpAddress = true;

            LaunchConfiguration buildConfiguration = asg.LaunchConfiguration;
            buildConfiguration.Condition = buildServerCondition;

            buildConfiguration.AlwaysReplace = true;
            activeDirectory.AddToDomain(buildConfiguration);

            buildConfiguration.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("SecurityGroupForRdpToBuild");
            buildConfiguration.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            buildConfiguration.RootDevice.Size = 100;
            var share = buildConfiguration
                .Deployments.Add(new WindowsShare("backup", "c://backup",v.VpcCidrBlock));

            var buildDeploymentAgainstBuildGroup = AddTfsBuildTier(
                buildConfiguration,
                tfsDeploymentOnTfs,
                buildServerCondition,
                activeDirectory,
                tfsInstance);
            // give access to the build server (on build) to the two subnets
            buildDeploymentAgainstBuildGroup.Connect(activeDirectory.VpcSettings.Subnets.First(), activeDirectory.VpcSettings.Subnets.Last());

            //AutoScalingGroup developerGroup = v.AddNew<AutoScalingGroup>("DeveloperGroup");
            //developerGroup.Condition = null;
            //foreach (Subnet vSubnet in v.Subnets)
            //{
            //    developerGroup.VpcZoneIdentifier.Add(vSubnet);
            //}

            //developerGroup.LaunchConfiguration = developerGroup.AddNew<LaunchConfiguration, LaunchConfigurationOptions>(
            //    LaunchConfigurationOptions.Windows | LaunchConfigurationOptions.AssociatePublicIpAddress,
            //    "DeveloperConfiguration");


            //LaunchConfiguration developerConfiguration = developerGroup.LaunchConfiguration;

            //AddCloudFormationPolicy(developerConfiguration);
            //developerConfiguration.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>("SecurityGroupForRdpToDeveloper");
            //developerConfiguration.RootDevice.Size = 100;
            //developerConfiguration.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;

            //developerConfiguration.Deployments.Add( new Chrome>($"Chrome{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new AwsCli>($"AwsCliOn{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new SandCastle>($"SandCastleOn{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new GitGui>($"GitGui{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new DotNetFrameworkSdk>($"DotNetFrameworkSdk{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new Bisque.EC2.Instances.Configuration.Deploy.Redgate.SmartAssembly>($"SmartAssemblyOn{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new TypeMockDeveloper>($"TypeMockDeveloper{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new Node>($"NodeOn{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new TfxCli>($"TfxCliOn{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new Vs>($"VsOn{developerConfiguration.Key}");
            //developerConfiguration.Deployments.Add( new ReSharper>($"ReSharperOn{developerConfiguration.Key}");
            //activeDirectory.AddToDomain(developerConfiguration);

            //var forceBuildOnDev = this.AddNew<StringParameter>("InstallBuildOnDev");
            //((Interface)this.Metadata[Interface.InterfaceKey]).ParameterGroups.SingleOrDefault(
            //    p => p.Label == developerConfiguration.Parameters.First().GroupLabel).Add(forceBuildOnDev.Key);
            //forceBuildOnTfs.Label = "Install Build Agent on Developer";
            //forceBuildOnTfs.Description = "Enter any non-zero length string to install build agent on Developer.";

            //var conditionBuildOnDev =
            //    this.Add(new Condition()
            //    {
            //        Key = Resource.NormalizeKey(InstallBuildOnTfsConditionKey),
            //        Value = new OrFunction(new NotFunction(new EqualsFunction(forceBuildOnDev, string.Empty)), 
            //            new AndFunction(new NotFunction(tfsServerInstanceCondition), new NotFunction(sqlServerInstanceCondition), new NotFunction(buildServerCondition)))
            //    });

            //TeamFoundationBuild buildOnDeveloper = AddTfsBuildTier(developerConfiguration, applicationTier, conditionBuildOnDev, activeDirectory, sql);

            //developerConfiguration.ImageIdDeploymentDemands.Add(Node.DemandName);
            //developerConfiguration.ImageIdDeploymentDemands.Add(SandCastle.DemandName);
            //developerConfiguration.ImageIdDeploymentDemands.Add(EC2.Instances.Configuration.Deploy.Redgate.SmartAssembly.DemandName);
            //developerConfiguration.ImageIdDeploymentDemands.Add(TfxCli.DemandName);
            //developerConfiguration.ImageIdDeploymentDemands.Add(Vs.DemandName);
            //developerConfiguration.ImageIdDeploymentDemands.Add(AwsCli.DemandName);

            //open sqlserver port up to the subnets
            sqlServerApplicationDeployment.Connect(activeDirectory.VpcSettings.Subnets.First(), activeDirectory.VpcSettings.Subnets.Last());
            // give access to the TFS application server (on Tfs) to the two subnets
            tfsDeploymentOnTfs.Connect(activeDirectory.VpcSettings.Subnets.First(), activeDirectory.VpcSettings.Subnets.Last());
            // give access to the TFS application server (on Sql) to the two subnets
            applicationTier.Connect(activeDirectory.VpcSettings.Subnets.First(), activeDirectory.VpcSettings.Subnets.Last());
        }

        private static TeamFoundationServerApplicationTierConfigure AddTfsApplicationTier(Instance addTfsApplicationTierTo, 
            MicrosoftAd activeDirectory, 
            Condition conditionForApplicationTier, 
            Instance sqlInstance, 
            SqlServer sqlServerApplicationDeployment, 
            WaitCondition waitConditionTfsApplicationTier, 
            StringParameter tfsVersionParameter)
        {
            if (waitConditionTfsApplicationTier == null)
                throw new ArgumentNullException(nameof(waitConditionTfsApplicationTier));

            var waitForSql = new WaitFor(sqlServerApplicationDeployment.WaitCondition)
            {
                Key = $"WaitForSqlDeploymentOn{addTfsApplicationTierTo.Key}"
            };
            addTfsApplicationTierTo.Deployments.Add(waitForSql);
            waitForSql.Condition = conditionForApplicationTier;

            TeamFoundationServerApplicationTierConfigure tfsApplicationTier = new TeamFoundationServerApplicationTierConfigure
            {
                Key = $"TfsApplicationTierOn{addTfsApplicationTierTo.Key}",
                Condition = conditionForApplicationTier,
                WaitCondition = waitConditionTfsApplicationTier
            };

            tfsApplicationTier.AttributesFile.Content.Tfs.SqlInstanceEndPoint = sqlInstance.Fqdn;
            tfsApplicationTier.AttributesFile.Content.Tfs.ApplicationTierFqdn = addTfsApplicationTierTo.Fqdn;
            tfsApplicationTier.AttributesFile.Content.Tfs.UserName = activeDirectory.AdminUser;
            tfsApplicationTier.AttributesFile.Content.Tfs.Password = activeDirectory.Password;
            tfsApplicationTier.AttributesFile.Content.Tfs.DefaultCollectionName = string.Empty;
            tfsApplicationTier.AttributesFile.Content.Tfs.Version = tfsVersionParameter;


            // add it to the instance
            addTfsApplicationTierTo.Deployments.Add(tfsApplicationTier);
            addTfsApplicationTierTo.ImageIdDeploymentDemands.Add(tfsApplicationTier.DemandName);

            return tfsApplicationTier;
        }

        private static TeamFoundationBuild AddTfsBuildTier(
            ILaunchConfiguration configurationToAddBuildTo,
            TeamFoundationServerApplicationTierConfigure tfsDeployment, 
            Condition conditionForBuildDeployments, 
            MicrosoftAd activeDirectory, 
            Instance instanceRunningTfs)
        {
            configurationToAddBuildTo.Deployments.Add( new AwsCli() { Condition = conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new TypeMockServer() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new TypeMockDeveloper() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new Node() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new TfxCli() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new SandCastle() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new DotNetFrameworkSdk() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new GitGui() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new Vs() {Condition= conditionForBuildDeployments});

            var addNetworkServiceToAdminGroup = configurationToAddBuildTo.Deployments.Add(
                new AddDirectoryUserToLocalGroup("\"NETWORK SERVICE\"", "Administrators")
                { Condition = conditionForBuildDeployments }
            );

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
