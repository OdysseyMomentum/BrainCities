using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Odyssey.API.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Odyssey.API.Tasks
{
    public class QueueManagerService : BackgroundService
    {
        private readonly IMqttClient mqttClient;
        private readonly string mqttURI;
        private readonly string mqttUser;
        private readonly string mqttPassword;
        private readonly string clientId;
        private readonly bool mqttSecure = false;
        private readonly int mqttPort;

        private readonly Dictionary<string, List<SensorData>> queue;
        private static readonly HttpClient client = new HttpClient();
        private static readonly DataWalletClient dwClient = new DataWalletClient();

        private readonly ILogger<QueueManagerService> logger;
        public QueueManagerService(ILogger<QueueManagerService> logger, IConfiguration configuration)
        {
            this.queue = new Dictionary<string, List<SensorData>>();

            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            mqttClient.UseApplicationMessageReceivedHandler(async e => { await HandleMessageReceived(e.ApplicationMessage); });
            mqttClient.UseDisconnectedHandler(async e => {
                logger.LogWarning("### DISCONNECTED FROM BROKER ###");
                while(!mqttClient.IsConnected)
                {
                    await Connect();
                }
                //subscribe when connected
                await ExecuteAsync(CancellationToken.None);
            });

            mqttClient.UseConnectedHandler(async e =>
            {
                logger.LogInformation("### CONNECTED WITH BROKER ###");
                if (await dwClient.AutenticateAsync().ConfigureAwait(false)) {
                    logger.LogInformation("### CONNECTED WITH BROKER ###");
                }
                else
                {
                    logger.LogError("Error connecting to DataWallet");
                }
            });

            this.logger = logger;

            clientId = "dashboard";
            mqttURI = configuration.GetValue("broker_ip", "");
            mqttUser = configuration.GetValue("client_username", "");
            mqttPassword = configuration.GetValue("client_password", "");
            int i;
            i = Convert.ToInt32(configuration.GetValue("broker_port", "1883"));
            mqttPort = i;

            dwClient.private_key = configuration.GetValue("dw_private_key", "");
            dwClient.pub_key = configuration.GetValue("dw_public_key", "");
            dwClient.receiverPublicDefaultKey = configuration.GetValue("dw_receiverPublicDefaultKey", "");
            dwClient.receiverdefaultFolderId = configuration.GetValue("dw_receiverdefaultFolderId", "");
        }

        public async Task<bool> Publish(string channel, string value)
        {
            if (mqttClient.IsConnected == false)
            {
                logger.LogWarning("publishing failed, trying to connect ...");
                await Connect();
                if (!mqttClient.IsConnected)
                {
                    logger.LogError("unable to connect to broker");
                    return false;
                }
            }

            var message = new MqttApplicationMessageBuilder()
                    .WithTopic(channel)
                    .WithPayload(value)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build();
            await mqttClient.PublishAsync(message);
            return true;
        }

        public async Task Connect()
        {
            var messageBuilder = new MqttClientOptionsBuilder()
              .WithClientId(clientId)
              .WithCredentials(mqttUser, mqttPassword)
              .WithTcpServer(mqttURI, mqttPort)
              .WithCleanSession();

            var options = mqttSecure ? messageBuilder.WithTls().Build() : messageBuilder.Build();

            logger.LogDebug("MQTT: connecting");
            await mqttClient.ConnectAsync(options, CancellationToken.None);
            logger.LogDebug("MQTT: connected");
        }

        private async Task HandleMessageReceived(MqttApplicationMessage applicationMessage)
        {
            var data = new SensorData
            {
                Value = Encoding.UTF8.GetString(applicationMessage.Payload)
            };

            Console.WriteLine($"{data.Timespan} | {applicationMessage.Topic} : {data.Value}");

            data.SensorId = applicationMessage.Topic.Split('/')[1];

            data.Key = applicationMessage.Topic.Split('/').Last();

            data.DataType = data.Key switch
            {
                "temperature" => DataType.Temperature,
                "humidity" => DataType.Light,
                _ => DataType.Unknown,
            };

            //Console.WriteLine("### RECEIVED APPLICATION MESSAGE ###");
            //Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(applicationMessage.Payload)}");
            //Console.WriteLine($"+ QoS = {applicationMessage.QualityOfServiceLevel}");
            //Console.WriteLine($"+ Retain = {applicationMessage.Retain}");
            //Console.WriteLine();

            var date = DateTime.UtcNow;
            var previousHour = date.AddMinutes(-1).ToString("yyyyMMddHHmm");
            var currentHour = $"{date.ToString("yyyyMMddHHmm")}";
            List<SensorData> currentPool;

            if (this.queue.ContainsKey(currentHour))
            {
                currentPool = this.queue[currentHour];
                currentPool.Add(data);
            }
            else
            {
                //new hour
                if (this.queue.ContainsKey(previousHour))
                {
                    var previousPool = this.queue[previousHour];
                    this.queue.Remove(previousHour);
                    if (previousPool.Count > 0)
                    {
                        if (await SendPoolToExternal($"{previousHour}.json" , previousPool)) // send to smartcontract
                        {
                            Console.WriteLine($"{previousHour} has been sent to DataWallet");
                            previousPool = null;
                        }
                        else
                        {
                            this.queue.Add(previousHour, previousPool);
                        }
                    }
                }
                currentPool = new List<SensorData>(new[] { data });
                this.queue.Add(currentHour, currentPool);
            }
        }

        private async Task<bool> SendPoolToExternal(string docName, object payload)
        {
            //var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");           
            //var response = await client.PostAsync($"https://smart-contract/documents?docname={docName}", content);
            //dynamic occ = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            //return response.IsSuccessStatusCode;

            dynamic req = new JObject();
            req.token = dwClient.Token;
            req.receiverPublicKey = dwClient.receiverPublicDefaultKey;
            req.folderId = dwClient.receiverdefaultFolderId;
            req.fileName = docName;
            req.jsonData = JsonConvert.SerializeObject(payload);

            return dwClient.AddDocument(req);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("executing task");

            if (mqttClient.IsConnected == false)
            {
                logger.LogWarning("publishing failed, trying to connect ...");
                await Connect();
                if (!mqttClient.IsConnected)
                {
                    logger.LogError("unable to connect to broker");
                    return;
                }
            }

            string[] strTopics = { "sensor/0/temperature", "sensor/0/humidity" };

            MqttClientSubscribeOptions objSubOptions = new MqttClientSubscribeOptions();

            List<MqttTopicFilter> objTopics = new List<MqttTopicFilter>();

            foreach (string strTopic in strTopics)
            {
                MqttTopicFilter objAdd = new MqttTopicFilter
                {
                    Topic = strTopic
                };
                objTopics.Add(objAdd);
            }

            objSubOptions.TopicFilters = objTopics;
            await mqttClient.SubscribeAsync(objSubOptions).ConfigureAwait(false); //!!!!subscribe goes here!!!!

            var message = new MqttApplicationMessageBuilder()
                   //here it does send the message
                   .WithTopic("hc")
                   .WithPayload("I'm ok")
                   .WithExactlyOnceQoS()
                   .WithRetainFlag()
                   .Build();

            await mqttClient.PublishAsync(message);

            return;
        }
    }
}
