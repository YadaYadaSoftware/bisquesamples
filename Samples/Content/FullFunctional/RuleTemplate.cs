using System;
using System.Collections.Generic;
using System.Text;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.Event;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class RuleTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            this.AddNew<Rule>();
        }
    }
}
