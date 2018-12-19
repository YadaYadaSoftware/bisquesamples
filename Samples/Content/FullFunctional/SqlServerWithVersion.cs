using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SqlServerWithVersion : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var i = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewSubnet()
                .WithNewInstance()
                .WithElasticIp();
            i.BlockDeviceMappings.RootDevice.Size = 60;
            i.ImageIdDemands = AmiDemandsEnum.Windows2012R2 | AmiDemandsEnum.Bit64;
            i.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            var sql = i.Deployments.Add( new SqlServer());
            var version = this.Add(new StringParameter("SqlVersion"));
            version.AllowedValues.Add("2014");
            version.AllowedValues.Add("2016");
            sql.AttributesFile.Content.SqlServer.Version = version;
        }
    }
}
