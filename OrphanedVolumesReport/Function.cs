using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.ConfigEvents;

using Amazon.EC2.Model;
using Amazon.SimpleNotificationService.Model;
using Amazon.EC2;
using Amazon.SimpleNotificationService;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace OrphanedVolumesReport
{
    public class Function
    {
        public string snsTopicARN = Environment.GetEnvironmentVariable("SNSTopic");
        public string cwNamespace = Environment.GetEnvironmentVariable("Namespace");

        public async Task<string> FunctionHandler(ConfigEvent inputEvent, ILambdaContext context)
        {
            try
            {
                context.Logger.LogLine("Started execution of Orphaned Volumes Report");

                //Initialize variables
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                int countVolumes = 0;

                //Create the EC2 Client
                AmazonEC2Client client = new AmazonEC2Client();
                //Create the request
                DescribeVolumesRequest volRequest = new DescribeVolumesRequest();
                volRequest.Filters = new List<Filter>() { new Filter() { Name = "status", Values = new List<string>() { "available" } } };
                //Execute the request
                DescribeVolumesResponse volResponse = await client.DescribeVolumesAsync(volRequest);

                //Iterate over the results
                foreach (Volume vol in volResponse.Volumes)
                {
                    sb.AppendLine("**** Found Orphaned Volume ****");
                    sb.AppendLine("Volume ID: " + vol.VolumeId);
                    sb.AppendLine("Size: " + vol.Size);
                    sb.AppendLine("Create Time: " + vol.CreateTime);
                    countVolumes++;
                }

                //Log to console
                context.Logger.Log(sb.ToString());
                //Send SNS Alert if any volumes were found 
                if (sb.ToString().Length > 0)
                {
                    await SendSNSAlert(sb.ToString());
                }
                //Log CloudWatch Metric
                await recordCloudWatchMetric(countVolumes);
            }
            catch (Exception e)
            {
                //Exceptions are logged to cloudwatch logs and rethrown to 'fail' the function
                //Set up an alert on the Lambda function failed event in CloudWatch to be notified of errors
                context.Logger.LogLine(e.InnerException.ToString());
                throw e;
            }
            return "Complete";
        }

        private async Task SendSNSAlert(string alertMessage)
        {
            Console.WriteLine("--Send SNS ALert--");
            String alertSubject = String.Format("Orphaned Volumes Report: {0:MM/dd/yyyy}", DateTime.Now);
            AmazonSimpleNotificationServiceClient snsClient = new AmazonSimpleNotificationServiceClient();
            PublishRequest request = new PublishRequest(snsTopicARN, alertMessage, alertSubject);
            await snsClient.PublishAsync(request);
        }

        private async Task recordCloudWatchMetric(int numVolumes)
        {
            Console.WriteLine("--Record CloudWatchMetric--");
            AmazonCloudWatchClient client = new AmazonCloudWatchClient();
            PutMetricDataRequest request = new PutMetricDataRequest();
            request.MetricData = new List<MetricDatum>() { new MetricDatum() { MetricName = "TotalOrphanedVolumes",
                                                                            Timestamp = DateTime.Now,
                                                                            Unit = "Count",
                                                                            Value = numVolumes } };
            request.Namespace = cwNamespace;
            await client.PutMetricDataAsync(request);
        }
    }
}
