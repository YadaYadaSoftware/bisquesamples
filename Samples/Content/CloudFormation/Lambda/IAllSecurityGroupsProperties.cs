using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Networking;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.Lambda
{
    public interface IAllSecurityGroupsProperties
    {
        SecurityGroup AppendedItem { get; set; }
        CloudVariant List { get; set; }
    }
}