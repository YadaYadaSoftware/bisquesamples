using System.Linq;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Resources;
using YadaYada.Bisque.Aws.CodeCommit;
using YadaYada.Bisque.Aws.Lambda;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class RepoSample : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var z = this.Add(new Repository("Bisque"));
            z = this.Add(new Repository("BubbleBoy") {DependsOn = { z }} );
            z = this.Add(new Repository("Chef") { DependsOn = { z } });
            z = this.Add(new Repository("InfralutionAuthServer") { DependsOn = { z } });
            z = this.Add(new Repository("InfralutionLicensing") { DependsOn = { z } });
            z = this.Add(new Repository("LicenseServer") { DependsOn = { z } });
            z = this.Add(new Repository("Marketplace") { DependsOn = { z } });
            z = this.Add(new Repository("NugetServer") { DependsOn = { z } });
            z = this.Add(new Repository("Shipping") { DependsOn = { z } });
            z = this.Add(new Repository("TfsUtility") { DependsOn = { z } });
            z = this.Add(new Repository("TfxTasks") { DependsOn = { z } });
            z.Triggers.Add(new Repository.LambdaTrigger(new Function(),"SampleTrigger"));
            this.Resources.ToList().ForEach(r=>r.Value.DeletionPolicy = Resource.DeletePolicy.Retain);

        }
    }
}
