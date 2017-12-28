# OrphanedVolumesReport
An automated alert that lists your orphaned volumes

## Description

This solution consists of an AWS Lambda function written in C# (.NET Core 1.0) that makes API calls to retrieve all 'available' (unattached) volumes, and publishes those details to an SNS Topic.  An Administrator can subscribe to the topic to receive this alert. Additionally, the function will record the number of orphaned volumes in a custom CloudWatch metric, so a trend can be observed over time.  The .yaml template is an AWS Serverless Application Model (SAM) template that creates the necessarily IAM role and policy, and the SNS Topic for publishing.  The email address for alerts, the custom CloudWatch namespace, and the schedule frequency for the function to be run can all be specified via parameters (default frequency is once per day). 

### Source

https://github.com/haider-cloud/OrphanedVolumesReport/

### License

MIT License
