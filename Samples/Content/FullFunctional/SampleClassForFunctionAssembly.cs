using Amazon.Lambda.Core;
using YadaYada.Bisque.Annotations;
using YadaYada.Bisque.Annotations.Lambda;
using YadaYada.Bisque.Aws.Lambda;

namespace YadaYada.Bisque.Aws.Samples.Content.FullFunctional
{
    public class SampleClassForFunctionAssembly
    {
        public void SampleMethod(ILambdaContext context)
        {

        }

        [NoFunction]
        public void NoFunctionSampleMethod(ILambdaContext context)
        {

        }
    }
}