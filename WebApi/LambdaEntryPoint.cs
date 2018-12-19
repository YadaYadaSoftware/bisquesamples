using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using YadaYada.Bisque.Annotations;
using YadaYada.Bisque.Annotations.Lambda;

namespace WebApi
{
    /// <summary>
    /// This class extends from APIGatewayProxyFunction which contains the method FunctionHandlerAsync which is the 
    /// actual Lambda function entry point. The Lambda handler field should be set to
    /// 
    /// WebApi::WebApi.LambdaEntryPoint::FunctionHandlerAsync
    /// </summary>
    public class LambdaEntryPoint : Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction
    {
        /// <summary>
        /// The builder has configuration, logging and Amazon API Gateway already configured. The startup class
        /// needs to be configured in this method using the UseStartup<>() method.
        /// </summary>
        /// <param name="builder"></param>
        protected override void Init(IWebHostBuilder builder)
        {
            builder
                .UseStartup<Startup>();
        }

        [Function(FunctionAttribute.TypeOfFunction.Lambda,commaDelimitedPolicies:"AWSLambdaFullAccess,AWSLambdaVPCAccessExecutionRole")]
        public override Task<APIGatewayProxyResponse> FunctionHandlerAsync(APIGatewayProxyRequest request, ILambdaContext lambdaContext)
        {
            return base.FunctionHandlerAsync(request, lambdaContext);
        }
    }
}
