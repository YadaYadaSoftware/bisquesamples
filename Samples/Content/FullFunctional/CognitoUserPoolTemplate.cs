using YadaYada.Bisque.Aws.ApiGateway;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.Cognito;
using YadaYada.Bisque.Aws.Iam.Roles;
using YadaYada.Bisque.Aws.System.IO;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class CognitoUserPoolTemplate : Template
    {
        protected override void InitializeTemplate()
        {
            base.InitializeTemplate();
            var pool = this.AddNew<UserPool>();
            pool.Name = "My User Pool";
            pool.Schema.Add(new UserPool.UserPoolProperties.SchemaAttribute() {Name = "email",Required = true});
            var user = this.AddNew<UserPoolUser>();
            user.Username = "noone@nowhere.com";
            user.UserPool = pool;
            user.DesiredDeliveryMediums.Add("EMAIL");
            user.UserAttributes.Add(new UserPoolUser.UserAttribute() {Name = "email", Value = "hounddog@gmail.com"});
            var app = this.AddNew<UserPoolClient>();
            app.ClientName = "Xyz";
            app.UserPool = pool;

            var role = this.AddNew<LambdaExecutionRole>();

            var group = this.AddNew<UserPoolGroup>();
            group.UserPool = pool;
            group.GroupName = "GroupName";
            group.Role = role;

            var userToGroup = this.AddNew<UserPoolUserToGroupAttachment>();
            userToGroup.DependsOn.Add(user, pool, group);
            userToGroup.UserPool = pool;
            userToGroup.UserName = user.Username;
            userToGroup.GroupName = group.GroupName;

            var restApi = this.AddNew<RestApi>();
            restApi.Name = "MyRestApi";

            var authorizer = this.AddNew<Authorizer>();
            authorizer.Name = "Authorizer1";
            authorizer.AuthType = "COGNITO_USER_POOLS";
            authorizer.IdentitySource = "method.request.header.name";
            authorizer.RestApi = restApi;
            authorizer.ProviderArns.Add(pool.GetArn());

            var resource = this.AddNew<ApiGateway.Resource>();
            resource.Parent = restApi.RootResourceId();
            resource.PathPart = "{proxy+}";
            resource.RestApi = restApi;

            var method = this.AddNew<Method>();
            method.HttpMethod = "GET";
            method.ResourceId = resource;
            method.RestApi = restApi;
            method.AuthorizationType = "COGNITO_USER_POOLS";
            method.Authorizer = authorizer;
            method.Integration.Type = "AWS";
            method.Integration.IntegrationHttpMethod = "GET";
            method.Integration.Uri = "arn:aws:apigateway:us-east-1:lambda:path/2015-03-31/functions/arn:aws:lambda:us-east-1:768033286672:function:pipeline-labelmaker-maste-ApiLambdaEntryPointFunct-1SKWBV31DO1XV/invocations";

            //var serverlessApi = this.AddNew<Serverless.Api>();
            //serverlessApi.StageName = "Stage1";
            //serverlessApi.DefinitionBody = JsonConvert.SerializeObject(File.ReadAllText(@"C:\Users\Administrator\source\repos\labelmaker\Api\bin\Debug\netcoreapp2.1\swagger.json"));

#if DEBUG
            RestApi.ScanAssemblyForWebApi(this, new WindowsFileInfo("../../../../WebApi/bin/Debug/netcoreapp2.1/WebApi.dll"));
#else
            RestApi.ScanAssemblyForWebApi(this, new WindowsFileInfo("../../../../WebApi/bin/release/netcoreapp2.1/WebApi.dll"));
#endif


        }
    }
}
