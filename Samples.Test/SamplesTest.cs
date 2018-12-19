using System.ComponentModel;
using System.IO;
using System.Linq;
using Xunit;
using YadaYada.Bisque.Aws.CloudFormation;
using YadaYada.Bisque.Aws.CodePipeline;
using YadaYada.Bisque.Aws.CodeStar;
using YadaYada.Bisque.Aws.Cognito;
using YadaYada.Bisque.Aws.EC2.Instances;
using YadaYada.Bisque.Aws.EC2.Instances.Configuration;
using YadaYada.Bisque.Aws.EC2.Networking;
using YadaYada.Bisque.Aws.Lambda;
using YadaYada.Bisque.Aws.Samples.Content;
using YadaYada.Bisque.Aws.Samples.Content.CloudFormation;
using YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Instancing.Configuration.Deploy.Microsoft;
using YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Instancing.Configuration.Deploy.WordPressDotOrg;
using YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Networking;
using YadaYada.Bisque.Aws.Samples.Content.CloudFormation.EC2.Networking.Vpcing;
using YadaYada.Bisque.Aws.Samples.Content.CloudFormation.Lambda;
using YadaYada.Bisque.Aws.Samples.Content.FullFunctional;
using YadaYada.Bisque.Aws.Serverless;
using YadaYada.Bisque.Aws.System.IO;
using GetAmiId = YadaYada.Bisque.Aws.Samples.Content.FullFunctional.GetAmiId;

namespace Samples.Test
{
    
    public class SamplesTest
    {
        protected Template TestTemplate<TTemplateType>() where TTemplateType : Template, new()
        {
            var t = new TTemplateType();
            TemplateEngine.CreateTemplateFiles(t);
            var directory = t.Save();
            WindowsFileInfo templateFile = new WindowsFileInfo(Path.Combine(directory.FullName, $"{t.Key}{Template.TemplateExtension}"));
            Assert.True(templateFile.Exists);

            using (var reader = templateFile.OpenText())
            {
                var json = reader.ReadToEnd();

                Template t2 = Template.Load(json);
                Assert.True(t.EqualsAssert(t2, true));
                return t2;
            }
        }
        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves InstallBisqueTemplate outputs")]
        public void InstallBisqueTemplateTest()
        {
            TestTemplate<InstallBisqueTemplate>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves NugetServerTemplate outputs")]
        public void NugetServerTemplateTest()
        {
            TestTemplate<NugetServerTemplate>();
            
        }

        [Fact]
        [Trait("Category","Slow")]
        [Trait("Category", "Samples")]
        /// "Proves CreateAmiTemplate outputs")]
        public void CreateAmiTemplateTest()
        {
            TestTemplate<CreateAmiTemplate>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves GetInstanceStateTemplate outputs")]
        public void GetInstanceStateTemplateTest()
        {
            TestTemplate<GetInstanceStateTemplate>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves LookAmazonMachineImageIDsTemplate outputs")]
        public void LookAmazonMachineImageIDsTemplateTest()
        {
            TestTemplate<LookAmazonMachineImageIDsTemplate>();

        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves SimpleLambdaTemplate outputs")]
        public void SimpleLambdaTemplateTest()
        {
            TestTemplate<SimpleLambdaTemplate>();

        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves WaitForInstanceStateTemplate outputs")]
        public void WaitForInstanceStateTemplateTest()
        {
            TestTemplate<WaitForInstanceStateTemplate>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves VpcSampleShowingDefaultVpc outputs")]
        public void VpcSampleShowingDefaultVpcTest()
        {
            TestTemplate<VpcSamples>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves BisqueHelpServerTemplate outputs")]
        public void BisqueHelpServerTemplateTest()
        {
            TestTemplate<BisqueHelpServerTemplate>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves BisqueHelpServerTemplate outputs")]
        public void Deploy2TheCloudWebTemplateTest()
        {
            TestTemplate<Deploy2TheCloudWebTemplate>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves BisqueHelpServerTemplate outputs")]
        public void VisuallCppRuntimeTemplateTest()
        {
            TestTemplate<VisuallCppRuntimeTemplate>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves WordPressTemplate outputs")]
        public void WordPressTemplateTest()
        {
            TestTemplate<WordPressTemplate>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves SimpleSqlServer outputs")]
        public void SimpleSqlServerTest()
        {
            TestTemplate<SimpleSqlServer>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves SqlServerWithParameterizeDirectories outputs")]
        public void SqlServerWithParameterizeDirectoriesTest()
        {
            TestTemplate<SqlServerWithParameterizeDirectories>();
        }
        
        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves SandcastleTemplate outputs")]
        public void SandcastleTemplateTest()
        {
            TestTemplate<SandcastleTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves SmartAssemblyTemplate outputs")]
        public void SmartAssemblyTemplateTest()
        {
#if DEBUG
            //throw new NotSupportedException();
            //var t = new SmartAssemblyTemplate();
            //var s = TemplateEngine.SerializeObject(t);
            //var t2 = TemplateEngine.DeserializeObject<Template>(s);
            //var l = (Instance)t.Resources.Single(r => r.Value is Instance).Value;
            //var l2 = (Instance)t2.Resources.Single(r => r.Value is Instance).Value;
            //Init i = l.Init;
            //Init i2 = l2.Init;

            //for (int j = 0; j < i.ConfigSets.Count; j++)
            //{
            //    ConfigSet c = i.ConfigSets.ToList()[j].Value;
            //    ConfigSet c2 = i2.ConfigSets.ToList()[j].Value;
            //    for (int k = 0; k < c.Count; k++)
            //    {
            //        Config cc = c.ToList()[k].Value;
            //        Config cc2 = c2.ToList()[k].Value;

            //        Assert.AreEqual(cc.Commands.Count, cc2.Commands.Count);

            //        for (int m = 0; m < cc.Commands.Count; m++)
            //        {
            //            CommandConfig comm = cc.Commands[cc.Commands.Keys.ToList()[m]];
            //            CommandConfig comm2 = cc2.Commands[cc2.Commands.Keys.ToList()[m]];
            //            Assert.AreEqual(comm.GetHashCode(), comm2.GetHashCode());
            //            Assert.True(comm.EqualsAssert(comm2, true));
            //        }

            //        Assert.AreEqual(cc.Commands.GetHashCode(), cc2.Commands.GetHashCode());
            //        Assert.True(cc.Commands.EqualsAssert(cc2.Commands, true));
            //        Assert.True(cc.EqualsAssert(cc2, true), $"{cc.Key} is not equal");

            //    }
            //}

            //Assert.True(i.EqualsAssert(i2, true));
            //Assert.True(l.EqualsAssert(l2, true));
            //Assert.True(t.EqualsAssert(t2, true));
#endif
            TestTemplate<SmartAssemblyTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves TeamFoundationBuildTemplate outputs")]
        public void TeamFoundationBuildTemplateTest()
        {
            TestTemplate<TeamFoundationBuildTemplate>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves TeamFoundationServerTemplate outputs")]
        public void TeamFoundationServerTemplateTest()
        {
            TestTemplate<TeamFoundationServerTemplate>();
        }

        
        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves TeamFoundationServerByFunctionTemplate outputs")]
        public void TeamFoundationServerByFunctionTemplateTest()
        {
            var s = TestTemplate<TeamFoundationServerByFunctionTemplate>();
            var sqlServer = (Instance)s.Resources.Single(r=>r.Key=="Sql").Value;
            var x = sqlServer.ResourceMetadata
                .Where(c => c.Value is ConfigSet);
            var file = 
            sqlServer.Init
                .ConfigSets
                .Values
                .Any(c=>c.Values
                            .Any(f=>f.Files
                            .Any(k=>k.Key.EndsWith("InstallBuildOnSqlServer.bat")))) ;

            Assert.True(file);
            
            

        }

        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves RemoteDesktopSecurityGroup outputs")]
        public void RemoteDesktopSecurityGroupTest()
        {
            var t = TestTemplate<RemoteDesktopSecurityGroupTemplate>();
            SecurityGroup sg = t.Resources.First().Value as SecurityGroup;
            Assert.NotNull(sg.Vpc);
            Assert.True(sg.Vpc.IsSpecial);
            Assert.NotNull(sg.Vpc.ImportValue);
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves SqlServerWithVersion outputs")]
        public void SqlServerWithVersionTest()
        {
            TestTemplate<SqlServerWithVersion>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves DeveloperBuildMachineIndividualComponents outputs")]
        public void DeveloperBuildMachineIndividualComponentsTest()
        {
            TestTemplate<DeveloperBuildMachineIndividualComponents>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves DeveloperBuildMachineVisualStudio outputs")]
        public void DeveloperBuildMachineVisualStudioTest()
        {
            TestTemplate<DeveloperBuildMachineVisualStudio>();
        }

        [Fact]
        [Trait("Category","Slow")]
        [Trait("Category", "Samples")]
        /// "Proves ChromeTemplate outputs")]
        public void ChromeTemplateTest()
        {
            TestTemplate<ChromeTemplate>();
        }

        
        [Fact]
        [Trait("Category","Slow")]
        [Trait("Category", "Samples")]
        /// "Proves GetAmiId outputs")]
        public void GetAmiIdTest()
        {
            TestTemplate<GetAmiId>();
        }

        [Fact]
        [Trait("Category","Slow")]
        [Trait("Category", "Samples")]
        /// "Proves JustALambaExecutionRole outputs")]
        public void JustALambaExecutionRoleTest()
        {
            TestTemplate<JustALambaExecutionRole>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves ChromeTemplate outputs")]
        public void GitGuiTemplateTest()
        {
            TestTemplate<GitGuiTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves TeamFoundationServerCreateAmiTemplate outputs")]
        public void TeamFoundationServerCreateAmiTemplateTest()
        {
            TestTemplate<TeamFoundationServerCreateAmiTemplate>();
        }

        [Fact(Skip = "Failing")]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves LicenseServerTemplate outputs")]
        public void LicenseServerTemplateTest()
        {
            TestTemplate<LicenseServerTemplate>();
        }


        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves SimpleSqlServerRds outputs")]
        public void SimpleSqlServerRdsTest()
        {
            TestTemplate<SimpleSqlServerRds>();
        }

        //
        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves SqlServerPrepare outputs")]
        public void SqlServerPrepareTest()
        {
            TestTemplate<SqlServerPrepare>();
        }

        
        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves SqlServerInstall outputs")]
        public void SqlServerInstallTest()
        {
            TestTemplate<SqlServerInstall>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves SqlServerInstallWithActiveDirectory outputs")]
        public void SqlServerInstallWithActiveDirectoryTest()
        {
            TestTemplate<SqlServerInstallWithActiveDirectory>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves TeamFoundationStackAmis outputs")]
        public void TeamFoundationStackAmisTest()
        {
            TestTemplate<TeamFoundationStackAmis>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves VisualStudioTemplate outputs")]
        public void VisualStudioTemplateTest()
        {
            TestTemplate<VisualStudioTemplate>();
        }
        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves Development outputs")]
        public void DevelopmentTest()
        {
            var t = TestTemplate<Development>();
            Assert.True(t.Resources.ContainsKey("RemoteDesktopSecurityGroup1"));
            var resource = t.Resources.Values.SingleOrDefault(r=>r.Type=="");
            Assert.Null(resource);

        }



        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves WindowsShareTemplate outputs")]
        public void WindowsShareTemplateTest()
        {
            TestTemplate<WindowsShareTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves MySqlTemplate outputs")]
        public void MySqlTemplateTest()
        {
            TestTemplate<MySqlTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves SimpleDirectoryTemplate outputs")]
        public void SimpleDirectoryTemplateTest()
        {
            TestTemplate<SimpleDirectoryTemplate>();
        }
        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves MicrosoftAdTemplate outputs")]
        public void MicrosoftAdTemplateTest()
        {
            TestTemplate<MicrosoftAdTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves VpcPeeringConnectionTemplate outputs")]
        public void VpcPeeringConnectionTemplateTest()
        {
            TestTemplate<VpcPeeringConnectionTemplate>();
        }

        [Fact]
        [Trait("Category", "Samples")]
        /// "Proves RepoSample outputs")]
        public void RepoSampleTest()
        {
            TestTemplate<RepoSample>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves VpcPeeringToExistingVpc outputs")]
        public void VpcPeeringToExistingVpcTest()
        {
            TestTemplate<VpcPeeringToExistingVpc>();
        }

        [Fact]
        [Trait("Category","Slow")]
        
        [Trait("Category", "Samples")]
        /// "Proves AutoScalingRollingUpdateTemplate outputs")]
        public void AutoScalingRollingUpdateTemplateTest()
        {
            TestTemplate<AutoScalingRollingUpdateTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves Deploy2TheCloudDirectory outputs")]
        public void Deploy2TheCloudDirectoryTest()
        {
            var t = TestTemplate<Deploy2TheCloudDirectory>();
            Assert.False(t.Parameters.Any(p=>p.Key=="Deploy2TheCloudDirectoryVpc"));
        }


        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves RdsMySqlTemplate outputs")]
        public void RdsMySqlTemplateTest()
        {
            var t = TestTemplate<RdsMySqlTemplate>();
        }
        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves RdsMySqlTemplate outputs")]
        public void VpcAsParameterTest()
        {
            var t = TestTemplate<VpcAsParameter>();
        }
        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves ConditionalSizeBasedOnParm outputs")]
        public void ConditionalSizeBasedOnParmTest()
        {
            var t = TestTemplate<ConditionalSizeBasedOnParm>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        
        [Trait("Category", "Samples")]
        /// "Proves ConditionalBatchFileTemplate outputs")]
        public void ConditionalBatchFileTemplateTest()
        {
            TestTemplate<ConditionalBatchFileTemplate>();
        }

        
        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves SubnetWithNoVpc outputs")]
        public void SubnetWithNoVpcTest()
        {
            TestTemplate<SubnetWithNoVpc>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves SqlServerAgainstImportedMicrosoftAd outputs")]
        public void SqlServerAgainstImportedMicrosoftAd()
        {
            TestTemplate<SqlServerAgainstImportedMicrosoftAd>();
        }

        
        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves SqlServerWithRestoreFromS3 outputs")]
        public void SqlServerWithRestoreFromS3()
        {
            TestTemplate<SqlServerWithRestoreFromS3>();
        }
        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves SimpleSqsTemplate outputs")]
        public void SimpleSqsTemplate()
        {
            TestTemplate<SimpleSqsTemplate>();
        }
        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves SimpleS3BucketTemplate outputs")]
        public void SimpleS3BucketTemplate()
        {
            TestTemplate<SimpleS3BucketTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves SnsTopicTemplate outputs")]
        public void SnsTopicTemplateTest()
        {
            TestTemplate<SnsTopicTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves ServerlessFunctionTemplate outputs")]
        public void ServerlessFunctionTemplateTest()
        {
            TestTemplate<ServerlessFunctionTemplate>();
        }

        //CertificateTemplate
        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves CertificateTemplate outputs")]
        public void CertificateTemplateTest()
        {
            TestTemplate<CertificateTemplate>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves AcesImportDevEnvironment outputs")]
        public void AcesImportDevEnvironmentTest()
        {
            TestTemplate<AcesImportDevEnvironment>();
        }

        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves AcesImportDevEnvironment outputs")]
        public void DeveloperBuildMachineCreateAmiTemplateTest()
        {
            TestTemplate<DeveloperBuildMachineCreateAmiTemplate>();
        }


        [Fact]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        /// "Proves AcesImportDevEnvironment outputs")]
        public void SimpleCreateAmiTest()
        {
            TestTemplate<SimpleCreateAmi>();
        }
        [Fact(DisplayName = "FunctionAssemblyTemplateTest")]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        [Trait("Target", nameof(FunctionAssembly))]
        /// "Proves FunctionAssemblyTemplate outputs")]
        public void FunctionAssemblyTemplateTest()
        {
            var t = TestTemplate<FunctionAssemblyTemplate>();
            Assert.DoesNotContain(t.Resources.Values.OfType<ServerlessFunction>(), f =>f.Key.Contains("NoFunctionSampleMethod"));

        }
        [Fact(DisplayName = nameof(InstanceFluentTest))]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        [Trait("Target", nameof(Instance))]
        /// "Proves outputs")]
        public void InstanceFluentTest()
        {
            TestTemplate<InstanceFluentTemplate>();
        }

        [Fact(DisplayName = nameof(CodeStarProjectTest))]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        [Trait("Target", nameof(Project))]
        /// "Proves outputs")]
        public void CodeStarProjectTest()
        {
            TestTemplate<CodeStarTemplate>();
        }
        //RuleTemplate
        [Fact(DisplayName = nameof(RuleTest))]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        [Trait("Target", nameof(Project))]
        /// "Proves outputs")]
        public void RuleTest()
        {
            TestTemplate<RuleTemplate>();
        }

        [Fact(DisplayName = nameof(CodeBuildTemplateTest))]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        [Trait("Target", nameof(YadaYada.Bisque.Aws.CodeBuild.Project))]
        /// "Proves outputs")]
        public void CodeBuildTemplateTest()
        {
            var t = TestTemplate<CodeBuildTemplate>();
            var t2 = new CodeBuildTemplate();
            var pipeline = t2.Resources.Values.OfType<Pipeline>().Single();
            Pipeline.Stage<Stage.CreateChangeset, Stage.ExecuteChangeset> stage =
                pipeline.Properties.Stages.Last() as Pipeline.Stage<Stage.CreateChangeset, Stage.ExecuteChangeset>;
            Assert.Equal($"Deploy{Template.TemplateExtension}", stage.Action1.TemplateName);
        }
        [Fact(DisplayName = nameof(CognitoUserPoolTemplateTest))]
        [Trait("Speed", "Fast")]
        [Trait("Category", "Samples")]
        [Trait("Target", nameof(UserPool))]
        public void CognitoUserPoolTemplateTest()
        {
            TestTemplate<CognitoUserPoolTemplate>();
        }
        [Fact(DisplayName = nameof(EventSourceMappingTemplateTest))]
        public void EventSourceMappingTemplateTest()
        {
            var t2 = TestTemplate<EventSourceMappingTemplate>();
            t2.Save();
        }
        //
    }
}