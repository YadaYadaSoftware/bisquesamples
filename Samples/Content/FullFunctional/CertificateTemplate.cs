using YadaYada.Bisque.Aws.CertificateManager;
using YadaYada.Bisque.Aws.CloudFormation;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class CertificateTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var c = this.Add(new Certificate("Cert1"));
            c.Properties.DomainName = "x.deploy2the.cloud";
            c.Properties.DomainValidation.Add(
                new Certificate.CertificateProperties.DomainValidationOptions()
                {
                    DomainName = "x.deploy2the.cloud",
                    ValidationDomain = "deploy2the.cloud"
                });
        }
    }
}
