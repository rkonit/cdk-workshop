using Amazon.CDK;
using Amazon.CDK.AWS.CodeCommit;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.Pipelines;
using Constructs;
using System.Collections.Generic;

namespace CdkWorkshop
{
    public class WorkshopPipelineStack : Stack
    {
        public WorkshopPipelineStack(Construct parent, string id, IStackProps props = null) : base(parent, id, props)
        {
            // Creates a CodeCommit repository called 'WorkshopRepo'
            var repo = new Repository(this, "WorkshopRepo", new RepositoryProps
            {
                RepositoryName = "WorkshopRepo"
            });

            // The basic pipeline declaration. This sets the initial structure
            // of our pipeline
            var pipeline = new CodePipeline(this, "Pipeline", new CodePipelineProps
            {
                PipelineName = "WorkshopPipeline",

                // Builds our source code outlined above into a could assembly artifact
                Synth = new ShellStep("Synth", new ShellStepProps{
                    Input = CodePipelineSource.CodeCommit(repo, "main"),  // Where to get source code to build
                    Commands = new string[] {
                        "npm install -g aws-cdk",
                        "curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel LTS", // OS-specific install cmd
                        "dotnet build ./src",  // Language-specific build cmd
                        "npx cdk synth"  // synth command for NPM-based projects
                    }
                }),
            });

            var deploy = new WorkshopPipelineStage(this, "Deploy");
            var deployStage = pipeline.AddStage(deploy);

            deployStage.AddPost(new ShellStep("TestViewerEndpoint", new ShellStepProps{
                EnvFromCfnOutputs = new Dictionary<string, CfnOutput> {
                    { "ENDPOINT_URL", deploy.HCViewerUrl }
                },
                Commands = new string[] { "curl -Ssf $ENDPOINT_URL" }
            }));
            deployStage.AddPost(new ShellStep("TestAPIGatewayEndpoint", new ShellStepProps{
                EnvFromCfnOutputs = new Dictionary<string, CfnOutput> {
                    { "ENDPOINT_URL", deploy.HCEndpoint }
                },
                Commands = new string[] {
                    "curl -Ssf $ENDPOINT_URL/",
                    "curl -Ssf $ENDPOINT_URL/hello",
                    "curl -Ssf $ENDPOINT_URL/test"
                }
            }));
        }
    }
}
