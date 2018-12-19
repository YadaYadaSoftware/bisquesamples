using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration.Deploy.Sql;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda.AmiLookup;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class Development : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            this.Add(new StringParameter("DirectoryStackName"));

            //var devVpc = this.Add(new Vpc("DevelopmentVpc"))
            //    .WithInternetAccess()
            //    .HavingCidrBlockOf("10.0.3.0/24");

            //var devSubnet = devVpc.WithNewSubnet();
            //devSubnet.CidrBlock = "10.0.3.0/24";

            Vpc dbVpc = Vpc.Import("${DirectoryStackName}-VpcId");
            Subnet subnet = Subnet.Import("${DirectoryStackName}-Subnet1Id");
            subnet.Vpc = dbVpc;
            Instance workstation = this.AddNew<Instance>();
            workstation.Subnet = subnet;

            SecurityGroup databaseSecurityGroup = SecurityGroup.Import("${DirectoryStackName}-DatabaseAccessSecurityGroupId");
            workstation.SecurityGroups.Add(databaseSecurityGroup);
            var rdp = workstation.SecurityGroups.AddNew<RemoteDesktopSecurityGroup>();
            workstation.ImageIdDemands = AmiDemandsEnum.Windows2016Full;
            workstation.Deployments.Add(new SqlServer());




            //Subnet subnet1 = Subnet.Import("${DirectoryStackName}-Subnet1Id");
            //Subnet subnet2 = Subnet.Import("${DirectoryStackName}-Subnet2Id");



        }
    }
}
