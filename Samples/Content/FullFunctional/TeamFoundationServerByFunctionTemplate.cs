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
using YadaYada.Bisque.Aws.CodeCommit;
using YadaYada.Bisque.Aws.DirectoryService;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Amazon;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Build;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.EWSoftware;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Git;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Directory;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Tfs;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Microsoft.Windows.ActiveDirectory;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.NodeDotOrg;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.TypeMock;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.VisualStudio;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Iam;
using YadaYada.Bisque.Aws.Iam.Roles;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;
using YadaYada.Bisque.Aws.S3;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class TeamFoundationServerByFunctionTemplate : Template
    {
        const string TfsKey = "Tfs";
        const string SqlKey = "Sql";
        const string CreateServerOneConditionKey = "CreateServerOne";
        const string CreateServerTwoConditionKey = "CreateServerTwo";
        const string CreateBuildLaunchConfigurationConditionKey = "CreateBuildLaunchConfiguration";
        const string CreateDeveloperLaunchConfigurationConditionKey = "CreateDeveloperLaunchConfiguration";
        const string InstallTfsOnSqlServerConditionKey = "InstallTfsOnSqlServer";
        const string InstallBuildOnSqlServerConditionKey = "InstallBuildOnSqlServer";
        const string InstallBuildOnTfsConditionKey = "InstallBuildOnTfs";
        const string TfsApplicationWaitConditionKey = "TfsApplicationTier";
        protected override void InitializeTemplate()
        {
            var repository = this.Add(new Repository("Bisque"));
            repository = this.Add(new Repository("BubbleBoy") { DependsOn = { repository } });
            repository = this.Add(new Repository("Chef") { DependsOn = { repository } });
            repository = this.Add(new Repository("InfralutionAuthServer") { DependsOn = { repository } });
            repository = this.Add(new Repository("InfralutionLicensing") { DependsOn = { repository } });
            repository = this.Add(new Repository("LicenseServer") { DependsOn = { repository } });
            repository = this.Add(new Repository("Marketplace") { DependsOn = { repository } });
            repository = this.Add(new Repository("NugetServer") { DependsOn = { repository } });
            repository = this.Add(new Repository("Shipping") { DependsOn = { repository } });
            repository = this.Add(new Repository("TfsUtility") { DependsOn = { repository } });
            repository = this.Add(new Repository("TfxTasks") { DependsOn = { repository } });
            //this.Resources.Where(r=>r.Value is Repository)
            //    .ToList()
            //    .ForEach(r => r.Value.DeletionPolicy = Resource.DeletePolicy.Retain);

            var bucket = this.Add(new Bucket("CodeBucket")
            { VersionStatus = Bucket.VersioningConfiguration.VersioningConfigurationStatus.Enabled });

            

            var tfsName = new JoinFunction(JoinFunction.DelimiterChar.Hyphen, new StackName(), "tfs");
            var sqlName = new JoinFunction(JoinFunction.DelimiterChar.Hyphen, new StackName(), "sql");

            StringParameter directoryStackNameParameter = 
                this.Add(new StringParameter("DirectoryStackName")
                {
                    GroupLabel = Template.ParametersOverviewGroupLabel,
                    Description = "Name of stack to import from.",
                    Label = "Import Stack",
                    MinLength = 1
                });


            Vpc importedVpc = Vpc.Import($"${{{directoryStackNameParameter.Key}}}-VpcId");

            MicrosoftAd activeDirectory = MicrosoftAd.Import(
                $"${{{directoryStackNameParameter.Key}}}-DirectoryAlias",
                $"${{{directoryStackNameParameter.Key}}}-DirectoryName", 
                $"${{{directoryStackNameParameter.Key}}}-DirectoryPassword");

            Subnet subnet1 = Subnet.Import($"${{{directoryStackNameParameter.Key}}}-Subnet1");
            Subnet subnet2 = Subnet.Import($"${{{directoryStackNameParameter.Key}}}-Subnet2");
            subnet1.Vpc = importedVpc;
            subnet2.Vpc = importedVpc;
                        // allow rdp access
            var remoteDesktopSecurityGroup = this.AddNew<RemoteDesktopSecurityGroup>();
            remoteDesktopSecurityGroup.Vpc = importedVpc;

            foreach (var keyValuePair in remoteDesktopSecurityGroup.Parameters.Values)
            {
                keyValuePair.GroupLabel = Template.ParametersOverviewGroupLabel;
            }

            // create the sql server instance
            Instance sql = this.Add(new Instance(SqlKey)
            {
                Subnet = subnet1,
                ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64,
                Deployments = {new DisableFirewall()},
            });
            sql.WithElasticIp();

            var sqlServerInstanceCondition =
                this.Add(new Condition(Resource.NormalizeKey(CreateServerOneConditionKey))
                {
                    Value = new NotFunction(new EqualsFunction(new ReferenceFunction((CloudVariant) sql.InstanceType), string.Empty))
                });
            sql.SecurityGroups.Add(remoteDesktopSecurityGroup);
            sql.Condition = sqlServerInstanceCondition;

            sql.RootDevice.Size = 200;
            sql.RootDevice.DeleteOnTermination = false;
            //SQLUSERDBDIR = "d:\UserDbData"
            var b = sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 200);
            b.DeleteOnTermination = false;
            //; Default directory for the Database Engine user database logs.
            //SQLUSERDBLOGDIR = "e:\UserDbLog"
            b = sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 200);
            b.DeleteOnTermination = false;
            //; Directories for Database Engine TempDB files.
            //SQLTEMPDBDIR = "f:\TempDbData"
            b = sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 20);
            b.DeleteOnTermination = false;
            //; Directory for the Database Engine TempDB log files.
            //SQLTEMPDBLOGDIR = "g:\TempDbLog"
            b = sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 20);
            b.DeleteOnTermination = false;
            //; Default directory for the Database Engine backup files.
            //SQLBACKUPDIR = "h:\Backup"
            b = sql.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 40);
            b.DeleteOnTermination = false;

            activeDirectory.AddToDomain(sql, sqlName);


            // install sql server
            SqlServer sqlServerConfigure = sql.Deployments.Add( new SqlServer());
            sqlServerConfigure.Version = "2016";
            sqlServerConfigure.InstallFromPrepared = true;
            sqlServerConfigure.AttributesFile.Content.SqlServer.DataDirectory = "d:\\sql";
            sqlServerConfigure.AttributesFile.Content.SqlServer.LogDirectory = "e:\\log";
            sqlServerConfigure.AttributesFile.Content.SqlServer.BackupDirectory = "f:\\backup";
            sql.ImageIdDeploymentDemands.Add(sqlServerConfigure.DemandName);


            Instance tfs = this.Add(new Instance(TfsKey)
            {
                ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64,
                BlockDeviceMappings = { new BlockDeviceMapping(100)},
                Subnet = subnet1
            });
            tfs.WithElasticIp();
            tfs.SecurityGroups.Add(remoteDesktopSecurityGroup);



            // create the TFS application server 
            tfs.Condition = this.Add(new Condition(Resource.NormalizeKey(CreateServerTwoConditionKey))
                {
                    Value = new NotFunction(new EqualsFunction(new ReferenceFunction((CloudVariant)tfs.InstanceType), string.Empty))
                });


            //// create the build server group
            //var asg = this.AddNew<AutoScalingGroup>();
            //asg.VpcZoneIdentifier.Add(subnet1);
            //asg.VpcZoneIdentifier.Add(subnet2);

            //var buildServerCondition = this.Add(
            //    new Condition(Resource.NormalizeKey(CreateBuildLaunchConfigurationConditionKey))
            //{
            //    Value = new NotFunction(new EqualsFunction(new ReferenceFunction(asg.MinSize), "0"))
            //});

            //asg.Condition = buildServerCondition;



            // cname record of tfs to sql
            var conditionApplicationTierOnSql =
                this.Add(new Condition(Resource.NormalizeKey(InstallTfsOnSqlServerConditionKey))
                {
                    Value = new AndFunction(
                                sqlServerInstanceCondition.Value, 
                                new EqualsFunction(new ReferenceFunction((CloudVariant)tfs.InstanceType), string.Empty))});
            var addTfsRecord = sql.Deployments.Add(new AddCnameRecordToDirectory(tfsName, activeDirectory.Name,
                activeDirectory.AdminUser, activeDirectory.Password));
            addTfsRecord.Condition = conditionApplicationTierOnSql;


            YesNoParameter forceBuildOnSql = this.Add(new YesNoParameter("InstallBuildOnSql"));

            ((Interface)this.Metadata[Interface.InterfaceKey]).ParameterGroups.Single(p => 
                p.Label == sql.Parameters.First().Value.GroupLabel)
                .Parameters.Add(forceBuildOnSql.Key);

            forceBuildOnSql.Label = "Install Build Agent on SQL";

            YesNoParameter forceBuildOnTfs = this.Add(new YesNoParameter("InstallBuildOnTfs"));

            ((Interface)this.Metadata[Interface.InterfaceKey]).ParameterGroups.Single(
                p => p.Label == tfs.Parameters.First().Value.GroupLabel).Parameters.Add(forceBuildOnTfs.Key);
            forceBuildOnTfs.Label = "Install Build Agent on TFS";
            forceBuildOnTfs.Description = "Enter any non-zero length string to install build agent on TFS.";

            var conditionBuildOnSql =
                this.Add(new Condition(Resource.NormalizeKey(InstallBuildOnSqlServerConditionKey))
                {
                    Value = new OrFunction( new EqualsFunction(forceBuildOnSql,YesNoParameter.Yes),
                                            new AndFunction(
                                                sql.Condition, 
                                                new NotFunction(tfs.Condition)
                                                //, new NotFunction(buildServerCondition)
                                                ))
                });

            var conditionBuildOnTfs =
                this.Add(new Condition(Resource.NormalizeKey(InstallBuildOnTfsConditionKey))
                {
                    Value =  new OrFunction( new EqualsFunction(forceBuildOnTfs, YesNoParameter.Yes),
                                             //new AndFunction(
                                                tfs.Condition
                                                //, new NotFunction(buildServerCondition)
                                                //)
                                                )
                });

            var waitConditionTfsApplicationTier = this.Add(
                new WaitCondition(TfsApplicationWaitConditionKey)
                {
                    Condition = this.Add(
                        new Condition($"TfsOrSqlServer")
                        {
                            Value = new OrFunction(tfs.Condition, sql.Condition)
                        })
                });

                // clone the tfs application tier installation from the clonable instance
            var applicationTier = AddTfsApplicationTier(
                sql, 
                activeDirectory, 
                conditionApplicationTierOnSql, 
                sql, 
                sqlServerConfigure, 
                waitConditionTfsApplicationTier);
            
            activeDirectory.AddToDomain(tfs, tfsName);

            //var loadbalancer = tfs.WithLoadBalancer(
            //    subnet1,
            //    subnet2,
            //    Port.TeamFoundationServerHttp,
            //    "tfs",
            //    "deploy2the.cloud",
            //    TargetGroupProtocol.Http,
            //    new ProtocolPortMapping()
            //    {
            //        ListenerPort = Port.Https,
            //        ListenerProtocol = ListenerProtocol.Https
            //    });


            var tfsDeploymentOnTfs = AddTfsApplicationTier(
                tfs, 
                activeDirectory,
                null, 
                sql, 
                sqlServerConfigure, 
                waitConditionTfsApplicationTier);

            TeamFoundationBuild buildOnSql = AddTfsBuildTier(
                sql, 
                applicationTier, 
                conditionBuildOnSql, 
                activeDirectory,
                tfsName);

            //Assert.AreEqual(conditionBuildOnSql, buildOnSql.WaitCondition.Condition);

            TeamFoundationBuild buildOnTfs = AddTfsBuildTier(
                tfs, 
                applicationTier, 
                conditionBuildOnTfs, 
                activeDirectory,
                tfsName);


            //var update = new AutoScalingGroup.AutoScalingRollingUpdate();
            //update.WaitOnResourceSignals = true;
            //asg.UpdatePolicy = update;
            //update.MinInstancesInService = 1;
            //update.MaxBatchSize = 1;
            //update.PauseTime = "PT1H";



            //asg.LaunchConfiguration = asg.AddNew<LaunchConfiguration>();
            //asg.LaunchConfiguration.AssociatePublicIpAddress = true;

            //LaunchConfiguration buildConfiguration = asg.LaunchConfiguration;
            //buildConfiguration.Condition = buildServerCondition;

            //buildConfiguration.AlwaysReplace = true;
            //activeDirectory.AddToDomain(buildConfiguration);

            //buildConfiguration.SecurityGroups.Add(remoteDesktopSecurityGroup);
            //buildConfiguration.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            //buildConfiguration.RootDevice.Size = 100;
            //var share = buildConfiguration.Deployments.Add(
            //    new WindowsShare("backup", "c://backup", "10.0.0.0/0"));

            //var buildDeploymentAgainstBuildGroup = AddTfsBuildTier(
            //    buildConfiguration,
            //    tfsDeploymentOnTfs,
            //    buildServerCondition,
            //    activeDirectory,
            //    tfsName);

            

            sqlServerConfigure.Connect(tfs);
        }

        private static TeamFoundationServerApplicationTierConfigure AddTfsApplicationTier(
            Instance addTfsApplicationTierTo, 
            MicrosoftAd activeDirectory, 
            Condition conditionForApplicationTier, 
            Instance sqlInstance, 
            SqlServer sqlServerApplicationDeployment, 
            WaitCondition waitConditionTfsApplicationTier)
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

            tfsApplicationTier.Content.Tfs.SqlInstanceEndPoint = sqlInstance.Fqdn;
            tfsApplicationTier.Content.Tfs.ApplicationTierFqdn = addTfsApplicationTierTo.Fqdn;
            tfsApplicationTier.Content.Tfs.UserName = activeDirectory.AdminUser;
            tfsApplicationTier.Content.Tfs.Password = activeDirectory.Password;
            tfsApplicationTier.Content.Tfs.DefaultCollectionName = "YadaYada";


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
            CloudVariant tfsName)
        {
            var c = configurationToAddBuildTo.Deployments.Add( new AwsCli() {Condition = conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new TypeMockServer() {Condition = conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new TypeMockDeveloper() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new Node() {Condition = conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new TfxCli() {Condition= conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new SandCastle() {Condition=conditionForBuildDeployments});
            configurationToAddBuildTo.Deployments.Add( new GitGui() {Condition=conditionForBuildDeployments});
            var vs = new Vs("2015");
            configurationToAddBuildTo.Deployments.Add($"Vs2015On{configurationToAddBuildTo.Key}",vs);
            vs.Condition = conditionForBuildDeployments;

            var vs2017 = new Vs("2017");
            configurationToAddBuildTo.Deployments.Add($"Vs2017On{configurationToAddBuildTo.Key}", vs2017);
            vs.Condition = conditionForBuildDeployments;


            var addNetworkServiceToAdminGroup = configurationToAddBuildTo
                .Deployments
                .Add(new AddDirectoryUserToLocalGroup("Administrators","\"NETWORK SERVICE\"")
                {
                    Condition = conditionForBuildDeployments
                });

            WaitFor waitForTfs = new WaitFor(tfsDeployment.WaitCondition);
            waitForTfs.Key = $"WaitForTfsApplicationTierOn{configurationToAddBuildTo.Key}";
            waitForTfs.Condition = conditionForBuildDeployments;
            configurationToAddBuildTo.Deployments.Add(waitForTfs);

            var install = configurationToAddBuildTo.Deployments.Add(new InstallTfxTask(new JoinFunction(JoinFunction.DelimiterChar.None, "http://", tfsName, ":8080/tfs"),
                "http://deploy2the.cloud/nuget/nuget",
                "Bisque.Tfs",
                activeDirectory.AdminUser,
                activeDirectory.Password)
            {
                Condition = conditionForBuildDeployments
            });

            
            TeamFoundationBuild buildDeployment = new TeamFoundationBuild()
            {
                Condition = conditionForBuildDeployments
            };

            buildDeployment.AttributesFile.Content.Tfs.UserName = activeDirectory.AdminUser;
            buildDeployment.AttributesFile.Content.Tfs.Password = activeDirectory.Password;
            buildDeployment.AttributesFile.Content.Tfs.DefaultCollectionName = string.Empty;
            buildDeployment.AttributesFile.Content.Tfs.ApplicationTierFqdn = tfsName;


            configurationToAddBuildTo.Deployments.Add($"BuildDeploymentOn{configurationToAddBuildTo.Key}", buildDeployment);

            AddCloudFormationPolicy(configurationToAddBuildTo);

            buildDeployment.WaitCondition.Timeout = TimeSpan.FromHours(6);
            buildDeployment.WaitCondition.Condition = conditionForBuildDeployments;

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
