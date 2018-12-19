using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Rds;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SimpleSqlServerRds : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            Rds.DbInstance db = this.AddNew<Vpc>()
                .WithInternetAccess()
                .WithNewDbInstance(Engine.SqlServerExpress);

            db.Engine = Engine.SqlServerExpress;
        }
    }
}
