using Godot;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Text;

public class Main : Node2D
{
    private IManagedMqttClient _mqttClient;

    private const int _groupNumber = 5;
    private readonly string _subscriptionTopic = _groupNumber.ToString() + "/#";

    private AnimatedSprite _trafficLight1;
    private AnimatedSprite _trafficLight2;
    private Car1 _car1;
    private Car2 _car2;

    public override void _Ready()
    {
        _trafficLight1 = GetNode<AnimatedSprite>("TrafficLight1");
        _trafficLight2 = GetNode<AnimatedSprite>("TrafficLight2");
        _car1 = GetNode<Car1>("Car1");
        _car2 = GetNode<Car2>("Car2");

        _car1.Connect("carSensor", this, "HandleCarSensor");
        _car2.Connect("carSensor", this, "HandleCarSensor");

        Connect("tree_exiting", this, "HandleExit");

        StartMQTTClient();
    }

    public override void _Process(float delta)
    {
    }

    private async void StartMQTTClient()
    {
        _mqttClient = new MqttFactory().CreateManagedMqttClient();

        RegisterCallbacks();

        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithClientId("mqtt-cs")
                .WithTcpServer("broker.0f.nl", 8883)
                .WithTls())
            .Build();

        await _mqttClient.StartAsync(options);

        await _mqttClient.SubscribeAsync(
            new TopicFilterBuilder()
                .WithTopic(_subscriptionTopic)
                .WithAtLeastOnceQoS()
                .Build()
        );

        GD.Print("MQTT Client Started.");
        GD.Print("Subscribed To " + _subscriptionTopic + ".");
    }

    private void RegisterCallbacks()
    {
        _mqttClient.Connected += (s, e) =>
        {
            GD.Print("Connected To Broker.");
        };

        _mqttClient.ConnectingFailed += (s, e) =>
        {
            GD.Print("Couldn't Connect To Broker.");
            if (e.Exception != null)
            {
                GD.Print(e.Exception.Message);
            }

        };

        _mqttClient.Disconnected += (s, e) =>
        {
            GD.Print("Disconnected From Broker.");
        };

        _mqttClient.ApplicationMessageReceived += (s, e) =>
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = Encoding.Default.GetString(e.ApplicationMessage.Payload);

            GD.Print("Message Received: \n\t >>> " + topic + " : " + payload);

            HandleMQTTMessage(topic, payload);
        };
    }

    private void HandleMQTTMessage(string topic, string payload)
    {
        if (topic == _groupNumber.ToString() + "/motor_vehicle/1/light/1")
        {
            _trafficLight1.SetFrame(int.Parse(payload));
            if (payload == "0")
            {
                _car1.Stop();
            }
            else if (payload == "2")
            {
                _car1.Move();
            }
        }
        else if (topic == _groupNumber.ToString() + "/motor_vehicle/2/light/1")
        {
            _trafficLight2.SetFrame(int.Parse(payload));
            if (payload == "0")
            {
                _car2.Stop();
            }
            else if (payload == "2")
            {
                _car2.Move();
            }
        }
    }

    private async void HandleExit()
    {
        await _mqttClient.StopAsync();
        _mqttClient.Dispose();
    }

    private void HandleCarSensor(int carId, bool sensorState)
    {
        _mqttClient.PublishAsync(
            new ManagedMqttApplicationMessageBuilder()
                .WithApplicationMessage(
                    new MqttApplicationMessageBuilder()
                        .WithTopic(_groupNumber.ToString() + "/motor_vehicle/" + carId.ToString() + "/sensor/1")
                        .WithPayload(sensorState ? "1" : "0")
                        .WithAtLeastOnceQoS()
                        .Build()
                )
                .Build()
        );
    }
}
