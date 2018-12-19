using System;
using System.Collections.Generic;
using System.Text;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.Lambda;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation
{
    public class EventSourceMappingTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            this.AddNew<EventSourceMapping>().BatchSize = 100;
        }
    }
}
