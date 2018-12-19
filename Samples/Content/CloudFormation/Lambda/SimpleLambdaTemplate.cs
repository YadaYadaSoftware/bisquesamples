using Newtonsoft.Json;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CloudFormation.Functions;
using YadaYada.Bisque.Aws.CloudFormation.Outputs;
using YadaYada.Bisque.Aws.CloudFormation.Parameters;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Iam.Roles;
using YadaYada.Bisque.Aws.Lambda;

namespace YadaYada.Bisque.Aws.Samples.Content.CloudFormation.Lambda
{
    public class SimpleLambdaTemplate : Template
    {
        public class AllSecurityGroups : Custom<AllSecurityGroups.AllSecurityGroupsProperties>, IAllSecurityGroupsProperties
        {
            private AllSecurityGroups()
            {
                
            }
            public AllSecurityGroups(string key) : base(key)
            {
                
            }
            public class AllSecurityGroupsProperties : Custom.CustomProperties, IAllSecurityGroupsProperties
            {
                //[JsonConverter(typeof(TemplateItem.SpecialConverter))]
                public CloudVariant List
                {
                    get
                    { return this.GetValue<CloudVariant>();
                    }
                    set { this.SetValue(value);}
                }

                [JsonConverter(typeof(TemplateItem.SpecialConverter))]
                public SecurityGroup AppendedItem
                {
                    get { return this.GetValue<SecurityGroup>(); }
                    set { this.SetValue(value);}
                }
            }

            public SecurityGroup AppendedItem
            {
                get { return this.Properties.AppendedItem; }
                set { this.Properties.AppendedItem = value; }
            }

            public CloudVariant List
            {
                get { return this.Properties.List; }
                set { this.Properties.List = value; }
            }
        }
       
        protected override void InitializeTemplate()
        {
            var function = this.AddNew<Function>();
            var role = this.AddNew<LambdaExecutionRole>();
            var zipFile = new Function.ZipFileFunctionCode();
            function.Code = zipFile;
            zipFile.ZipFile = new JoinFunction(JoinFunction.DelimiterChar.None,
                "var response = require('cfn-response');",
            "exports.handler = function(event, context) {",
            "   var responseData = {Value: event.ResourceProperties.List};",
            "   responseData.Value.push(event.ResourceProperties.AppendedItem);",
            "   response.send(event, context, response.SUCCESS, responseData);",
            "};");
            function.Role = role;
            function.Runtime = Function.FunctionRuntime.NodeJs43;
            var split = this.AddNew<AllSecurityGroups>();
            split.ServiceToken = new GetAttributeFunction(function.Key, GetAttributeFunction.Attributes.Arn);
            var sg = this.AddNew<SecurityGroup>();
            split.AppendedItem = sg;
            var p = this.Add(new Parameter("ExistingSecurityGroups"));
            p.Type = ParameterType.ListAwsEc2SecurityGroupId;
            split.List = p;

            this.Outputs.Add("AllSecurityGroups",
                new Output("AllSecurityGroups")
                {
                    Value =
                new JoinFunction(JoinFunction.DelimiterChar.Comma, new GetAttributeFunction(split.Key, "Value"))
                });
        }
    }
}
