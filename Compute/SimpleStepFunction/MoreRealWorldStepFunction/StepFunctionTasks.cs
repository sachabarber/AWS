using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MoreRealWorldStepFunction
{
    public class StepFunctionTasks
    {
        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public StepFunctionTasks()
        {
        }


        public State Initial(State state, ILambdaContext context)
        {
            state.Message = $"Hello-{Guid.NewGuid().ToString()}";

            LogMessage(context, state.ToString());


            state.IsMale = state.Name.StartsWith("Mr") ? 1 : 0;


            // Tell Step Function to wait 5 seconds before calling 
            state.WaitInSeconds = 5;

            return state;
        }

        public State PrintMaleInfo(State state, ILambdaContext context)
        {
            LogMessage(context, "IS MALE");
            return state;
        }

        public State PrintFemaleInfo(State state, ILambdaContext context)
        {
            LogMessage(context, "IS FEMALE");
            return state;
        }


        public State Pass(State state, ILambdaContext context)
        {
            return state;
        }


        public State PrintInfo(State state, ILambdaContext context)
        {
            LogMessage(context, state.ToString());
            return state;
        }


        void LogMessage(ILambdaContext ctx, string msg)
        {
            ctx.Logger.LogLine(
                string.Format("{0}:{1} - {2}",
                    ctx.AwsRequestId,
                    ctx.FunctionName,
                    msg));
        }
    }
}
