//using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Odyssey.API.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Odyssey.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    //[Authorize]
    public partial class SensorsController : ControllerBase
    {
        const string clientId = "dashboard";
        private readonly ILogger<SensorsController> logger;
        private static readonly List<SensorData> _dataInMemoryStore = new List<SensorData>();
        private readonly IConfiguration configuration;

        public SensorsController(IConfiguration configuration, ILogger<SensorsController> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            if (_dataInMemoryStore.Count == 0)
            {
                _dataInMemoryStore.Add(new SensorData { DataType = DataType.Light });
            }
        }

        [HttpHead]
        [Route("action")]
        public async Task<ActionResult> PerformAction(string id = "0", string action = "action", string payload = "")
        {
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            var mqttURI = configuration.GetValue("broker_ip", "");
            var mqttUser = configuration.GetValue("client_username", "");
            var mqttPassword = configuration.GetValue("client_password", "");
            bool mqttSecure = false;
            int i;
            i = Convert.ToInt32(configuration.GetValue("broker_port", "1883"));
            var mqttPort = i;

            var messageBuilder = new MqttClientOptionsBuilder()
              .WithClientId(clientId)
              .WithCredentials(mqttUser, mqttPassword)
              .WithTcpServer(mqttURI, mqttPort)
              .WithCleanSession();

            var options = mqttSecure ? messageBuilder.WithTls().Build() : messageBuilder.Build();

            logger.LogDebug("MQTT: connecting");
            await mqttClient.ConnectAsync(options, CancellationToken.None);
            logger.LogDebug("MQTT: connected");

            if (mqttClient.IsConnected == false)
            {
                logger.LogError("publishing failed, not connected");
                return Problem("Not connected to the broker");
            }

            var message = new MqttApplicationMessageBuilder()
                    .WithTopic($"{clientId}/sensor/{id ?? "0"}/{action ?? "action"}")
                    .WithPayload(payload)
                    .WithExactlyOnceQoS()
                    //.WithRetainFlag()
                    .Build();
            await mqttClient.PublishAsync(message);

            return Ok();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<SensorData> CreateEntry(SensorData data)
        {
            data.Id = _dataInMemoryStore.Any() ?
                     _dataInMemoryStore.Max(p => p.Id) + 1 : 1;
            _dataInMemoryStore.Add(data);

            return CreatedAtAction(nameof(GetById), new { id = data.Id }, data);
        }

        [HttpGet]
        public ActionResult<List<SensorData>> GetAll()
        {
            return _dataInMemoryStore;
        }

        [HttpGet("{id}")]
        public ActionResult<SensorData> GetById(int id, bool tempFilter)
        {
            var data = _dataInMemoryStore.FirstOrDefault(p => p.Id == id && (!tempFilter || p.DataType == DataType.Light));

            if (data == null)
            {
                return NotFound();
            }

            return data;
        }

    }
}