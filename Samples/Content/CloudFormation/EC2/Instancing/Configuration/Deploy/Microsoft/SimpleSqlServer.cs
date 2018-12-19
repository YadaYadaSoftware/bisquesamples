using System;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Instancing.Configuration.Deploy.Microsoft
{
    public class SimpleSqlServer : Template
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
            sqlDeployment.AttributesFile.Content.SqlServer.DataDirectory = "d:\\sql";
            sqlDeployment.AttributesFile.Content.SqlServer.LogDirectory = "e:\\log";
            sqlDeployment.AttributesFile.Content.SqlServer.BackupDirectory = "f:\\backup";
            sqlDeployment.WaitCondition.Timeout = TimeSpan.FromMinutes(45);
        }
    }
}
