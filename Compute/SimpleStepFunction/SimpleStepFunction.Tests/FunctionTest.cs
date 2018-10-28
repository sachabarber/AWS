using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.StepFunctions;
using Amazon;
using Newtonsoft.Json;
using Amazon.StepFunctions.Model;
using System.Net;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.Runtime;
using System.Threading.Tasks;
using System.Threading;

namespace SimpleStepFunction.Tests
{
    public class FunctionTest
    {
        public FunctionTest()
        {
        }

        [Fact]
        public void TestGreeting()
        {
            TestLambdaContext context = new TestLambdaContext();

            StepFunctionTasks functions = new StepFunctionTasks();

            var state = new State
            {
                Name = "MyStepFunctions"
            };


            state = functions.Greeting(state, context);

            Assert.Equal(5, state.WaitInSeconds);
            Assert.Equal("Hello MyStepFunctions", state.Message);
        }

    }
}






    