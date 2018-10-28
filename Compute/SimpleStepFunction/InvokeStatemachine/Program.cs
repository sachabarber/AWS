    using Amazon;
    using Amazon.Extensions.NETCore.Setup;
    using Amazon.SecurityToken;
    using Amazon.SecurityToken.Model;
    using Amazon.StepFunctions;
    using Amazon.StepFunctions.Model;
    using Newtonsoft.Json;
    using SimpleStepFunction;
    using System;
    using System.Threading.Tasks;

    namespace InvokeStatemachine
    {
        class Program
        {
            static void Main(string[] args)
            {
                //The IAM Default user you are using here will need StepFunctionsFullAccess
                ExecuteStepFunctionUsingDefaultProfileWithIAMStepFunctionsFullAccessInIAMConsole();
                //ExecuteStepFunctionUsingAssumedExistingStateMachineRole();
                Console.ReadLine();
            }


            static void ExecuteStepFunctionUsingDefaultProfileWithIAMStepFunctionsFullAccessInIAMConsole()
            {
                var options = new AWSOptions()
                {
                    Profile = "default",
                    Region = RegionEndpoint.EUWest2
                };

                var amazonStepFunctionsConfig = new AmazonStepFunctionsConfig { RegionEndpoint = RegionEndpoint.EUWest2 };
                using (var amazonStepFunctionsClient = new AmazonStepFunctionsClient(amazonStepFunctionsConfig))
                {
                    var state = new State
                    {
                        Name = "MyStepFunctions"
                    };
                    var jsonData1 = JsonConvert.SerializeObject(state);
                    var startExecutionRequest = new StartExecutionRequest
                    {
                        Input = jsonData1,
                        Name = $"SchedulingEngine_{Guid.NewGuid().ToString("N")}",
                        StateMachineArn = "arn:aws:states:eu-west-2:464534050515:stateMachine:StateMachine-z8hrOwmL9CiG"
                    };
                    var taskStartExecutionResponse = amazonStepFunctionsClient.StartExecutionAsync(startExecutionRequest).ConfigureAwait(false).GetAwaiter().GetResult();
                }


                Console.ReadLine();
            }


            static void ExecuteStepFunctionUsingAssumedExistingStateMachineRole()
            {
                var options = new AWSOptions()
                {
                    Profile = "default",
                    Region = RegionEndpoint.EUWest2
                };

                var assumedRoleResponse = ManualAssume(options).ConfigureAwait(false).GetAwaiter().GetResult();
                var assumedCredentials = assumedRoleResponse.Credentials;
                var amazonStepFunctionsConfig = new AmazonStepFunctionsConfig { RegionEndpoint = RegionEndpoint.EUWest2 };
                using (var amazonStepFunctionsClient = new AmazonStepFunctionsClient(
                    assumedCredentials.AccessKeyId,
                    assumedCredentials.SecretAccessKey, amazonStepFunctionsConfig))
                {
                    var state = new State
                    {
                        Name = "MyStepFunctions"
                    };
                    var jsonData1 = JsonConvert.SerializeObject(state);
                    var startExecutionRequest = new StartExecutionRequest
                    {
                        Input = jsonData1,
                        Name = $"SchedulingEngine_{Guid.NewGuid().ToString("N")}",
                        StateMachineArn = "arn:aws:states:eu-west-2:464534050515:stateMachine:StateMachine-z8hrOwmL9CiG"
                    };
                    var taskStartExecutionResponse = amazonStepFunctionsClient.StartExecutionAsync(startExecutionRequest).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                Console.ReadLine();
            }


            public static async Task<AssumeRoleResponse> ManualAssume(AWSOptions options)
            {
                var stsClient = options.CreateServiceClient<IAmazonSecurityTokenService>();
                var assumedRoleResponse = await stsClient.AssumeRoleAsync(new AssumeRoleRequest()
                {
                    RoleArn = "arn:aws:iam::464534050515:role/SimpleStepFunction-StateMachineRole-1N638DC3RLA16",
                    RoleSessionName = "test"
                });

                return assumedRoleResponse;

            }
        }
    }
