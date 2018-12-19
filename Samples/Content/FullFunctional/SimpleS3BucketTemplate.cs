using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.Iam;
using YadaYada.Bisque.Aws.S3;
using YadaYada.Bisque.Aws.Sns;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SimpleS3BucketTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var topic = this.AddNew<Topic>();
            var policy = this.AddNew<Topic.TopicPolicy>();

            var s = new Statement()
            {
                Action = {"sns:Publish"},
                Resource = "*"
            };
            s.Principal = new AccountPrincipal() { Accounts = "*" };
            policy.Document.Statement.Add(s);
            policy.Topics.Add(topic);


            var b = this.AddNew<Bucket>();
            b.Notification.Topics.Add(new Bucket.NotificationConfiguration.TopicConfiguration()
            {
                Event = Bucket.NotificationConfiguration.EventType.ObjectCreatedAny,
                Topic = topic
            });
        }
    }
}