using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Mappings;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.EC2.Instances;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class ConditionalSizeBasedOnParm : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var stage = this.Add(new StringParameter("Stage"));
            stage.AllowedValues.Add("Prod");
            stage.AllowedValues.Add("Test");
            stage.AllowedValues.Add("Dev");

            //var c1 = this.AddNew<Condition>("ProdInstance");
            //c1.Value = new EqualsFunction(stage, "Prod");
            //var c2 = this.AddNew<Condition>("TestInstance");
            //c2.Value = new EqualsFunction(stage, "Test");
            //var c3 = this.AddNew<Condition>("DevInstance");
            //c3.Value = new EqualsFunction(stage, "Dev");

            var m = this.Add(new Mapping("InstanceType"));
            var p = m.AddLine("Prod");
            p.Add("Prod", "t2.large");
            p.Add("Test", "t2.small");
            p.Add("Dev", "t2.nano");

            var i = this.AddNew<Instance>();
            CloudVariant f = new FindInMapFunction(m.Key, stage, "Prod");
            i.KeyName = f;

        }
    }
}
