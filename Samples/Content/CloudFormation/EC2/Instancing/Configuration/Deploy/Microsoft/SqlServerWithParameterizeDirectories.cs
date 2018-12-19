using System;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Instancing.Configuration.Deploy.Microsoft
{
    public class SqlServerWithParameterizeDirectories : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            // create the sql server instance
            var sqlServerInstance = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();

            sqlServerInstance.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            var sqlDeployment = sqlServerInstance.Deployments.Add(new SqlServer());
            sqlServerInstance.BlockDeviceMappings.RootDevice.Size = 100;
            sqlServerInstance.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 25);
            sqlServerInstance.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 25);
            sqlServerInstance.BlockDeviceMappings.AddNew(VolumeType.GeneralPurpose, 5);
            var directory = new Parameter("SqlDataDirectory")
            {
                Type = ParameterType.String,
                Label = "SQLServer Data Directory"
            };
            this.Add(directory);
            sqlDeployment.Content.SqlServer.DataDirectory = directory;

            directory = new Parameter("SqlLogDirectory")
            {
                Type = ParameterType.String,
                Label = "SQLServer Log Directory"
            };
            this.Add(directory);

            sqlDeployment.AttributesFile.Content.SqlServer.LogDirectory = directory;

            directory = new Parameter("SqlBackupDirectory")
            {
                Type = ParameterType.String,
                Label = "SQLServer Backup Directory"
            };
            this.Add(directory);

            sqlDeployment.AttributesFile.Content.SqlServer.BackupDirectory = directory;
            sqlDeployment.WaitCondition.Timeout = TimeSpan.FromMinutes(45);
        }
    }
}