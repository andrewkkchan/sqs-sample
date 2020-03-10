using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.Consul;

namespace SQS_Sample
{
    class Program
    {
        static async Task<Secret<SecretData>> getSecret()
        {
            // Initialize one of the several auth methods.
            IAuthMethodInfo authMethod = new TokenAuthMethodInfo("s.pgAZ2CkI4CqPT0SJ7H5iQZOJ");

// Initialize settings. You can also set proxies, custom delegates etc. here.
            var vaultClientSettings = new VaultClientSettings("http://127.0.0.1:8200", authMethod);

            IVaultClient vaultClient = new VaultClient(vaultClientSettings);

// Use client to read a key-value secret.
            Secret<SecretData> kv2Secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync("secret/aws2");
            return kv2Secret;
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var kv2Secret = await getSecret();

            //the url for our queue
            var queueUrl = "https://sqs.eu-west-2.amazonaws.com/896009684607/sample_imagine";

            Console.WriteLine("Queue Test Starting!");

            Console.WriteLine("Creating Client and request");

//Create some Credentials with our IAM user
            var awsCreds = new BasicAWSCredentials((string) kv2Secret.Data.Data["access"],
                (string) kv2Secret.Data.Data["secret"]);

//Create a client to talk to SQS
            var amazonSQSClient = new AmazonSQSClient(awsCreds, Amazon.RegionEndpoint.EUWest2);

//Create the request to send
            var sendRequest = new SendMessageRequest();
            sendRequest.QueueUrl = queueUrl;
            sendRequest.MessageBody = "{ 'message' : 'hello world' }";

//Send the message to the queue and wait for the result
            Console.WriteLine("Sending Message");
            var sendMessageResponse = amazonSQSClient.SendMessageAsync(sendRequest).Result;

            Console.WriteLine("Receiving Message");

//Create a receive requesdt to see if there are any messages on the queue
            var receiveMessageRequest = new ReceiveMessageRequest();
            receiveMessageRequest.QueueUrl = queueUrl;

//Send the receive request and wait for the response
            var response = amazonSQSClient.ReceiveMessageAsync(receiveMessageRequest).Result;

//If we have any messages available
            if (response.Messages.Any())
            {
                foreach (var message in response.Messages)
                {
                    //Spit it out
                    Console.WriteLine(message.Body);

                    //Remove it from the queue as we don't want to see it again
                    var deleteMessageRequest = new DeleteMessageRequest();
                    deleteMessageRequest.QueueUrl = queueUrl;
                    deleteMessageRequest.ReceiptHandle = message.ReceiptHandle;

                    var result = amazonSQSClient.DeleteMessageAsync(deleteMessageRequest).Result;
                }
            }
        }
    }
}