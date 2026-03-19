using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace PGD
{
    public class M2Mqtt : MonoBehaviour
    {
        public static M2Mqtt instance;
        public M2MqttClient m2MqttClient;

        // MQTT setting
        protected MqttClient client;

        private string brokerAddress;
        private int brokerPort;
        private string brokerTopic;
        private string mqttUserName = null;
        private string mqttPassword = null;

        [Tooltip("Use encrypted connection")]
        public bool isEncrypted = false;
        [Tooltip("Connection timeout in milliseconds")]
        public int timeoutOnConnection = MqttSettings.MQTT_CONNECT_TIMEOUT;

        private List<MqttMsgPublishEventArgs> messageQueue1 = new List<MqttMsgPublishEventArgs>();
        private List<MqttMsgPublishEventArgs> messageQueue2 = new List<MqttMsgPublishEventArgs>();
        private List<MqttMsgPublishEventArgs> frontMessageQueue = null;
        private List<MqttMsgPublishEventArgs> backMessageQueue = null;

        private bool mqttClientConnectionClosed = false;
        private bool mqttClientConnected = false;

        void Awake()
        {
            instance = this;

            frontMessageQueue = messageQueue1;
            backMessageQueue = messageQueue2;
        }

        public void TestPublish()
        {
            client.Publish("urrobot/control", System.Text.Encoding.UTF8.GetBytes("Test message"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            Debug.Log("Test message published");
        }

        void Start()
        {
            //MQTTConfig mQTTConfig = new MQTTConfig();

            //mQTTConfig.brokerAddress = "220.90.135.156";
            //mQTTConfig.brokerPort = "62547";
            //mQTTConfig.brokerTopic = "hyundai/alarm/insert";
            //print("enter");
            //string json = JsonUtility.ToJson(mQTTConfig);
            //string fileName = "MQTTConfig";
            //string path = Application.dataPath + "/" + fileName + ".Json";
            //File.WriteAllText(path, json);

            string fileName = "MQTTConfig";
            string path = Application.dataPath + "/" + fileName + ".Json";
            string json = File.ReadAllText(path);

            MQTTConfig mqttConfig = JsonUtility.FromJson<MQTTConfig>(json);

            brokerAddress = mqttConfig.brokerAddress;
            brokerPort = mqttConfig.brokerPort;
            brokerTopic = mqttConfig.brokerTopic;

            //Connect(); // MQTT 연결 비활성화 (테스트용)
        }

        void Update()
        {
            ProcessMqttEvents();
        }

        protected virtual void ProcessMqttEvents()
        {
            SwapMqttMessageQueues();
            ProcessMqttMessageBackgroundQueue();
        }

        private void ProcessMqttMessageBackgroundQueue()
        {
            foreach (MqttMsgPublishEventArgs msg in backMessageQueue)
            {
                DecodeMessage(msg.Topic, msg.Message);
            }
            backMessageQueue.Clear();
        }

        private void DecodeMessage(string topic, byte[] message)
        {
            string msg = System.Text.Encoding.UTF8.GetString(message);
            if (msg != null)
            {
                UIManager.Instance.ChangeNoticeColor();
            }
            //m2MqttClient.ArrangeData(msg, topic); 
        }

        private void SwapMqttMessageQueues()
        {
            frontMessageQueue = frontMessageQueue == messageQueue1 ? messageQueue2 : messageQueue1;
            backMessageQueue = backMessageQueue == messageQueue1 ? messageQueue2 : messageQueue1;
        }

        public void Connect()
        {

            if (client == null || !client.IsConnected)
            {
                StartCoroutine(DoConnect());
            }
        }

        private IEnumerator DoConnect()
        {
            if (client == null)
            {
                try
                {
                    client = new MqttClient(brokerAddress, brokerPort, isEncrypted, null, null, isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                }
                catch (Exception e)
                {
                    client = null;
                    OnConnectionFailed(e.Message);
                    StartCoroutine(DoConnect());
                    yield break;
                }
            }
            else if (client.IsConnected)
            {
                yield break;
            }

            client.Settings.TimeoutOnConnection = timeoutOnConnection;

            string clientId = Guid.NewGuid().ToString();

            try
            {
                client.Connect(clientId, mqttUserName, mqttPassword);
            }
            catch (Exception e)
            {
                client = null;
                OnConnectionFailed(e.Message);
                StartCoroutine(DoConnect());
                yield break;
            }

            if (client.IsConnected)
            {
                client.ConnectionClosed += OnMqttConnectionClosed;
                client.MqttMsgPublishReceived += OnMqttMessageReceived;

                mqttClientConnected = true;
                SubscribeTopics();
                StartCoroutine(OnConnectionSuccess());
            }
        }

        private void OnConnectionFailed(string errorMessage)
        {
            Debug.LogWarning("Connection failed. " + errorMessage + " " + brokerAddress + " " + brokerPort);
        }

        private IEnumerator OnConnectionSuccess()
        {
            print("Connected to " + brokerAddress + " " + brokerPort.ToString());
            yield return new WaitForSeconds(1.5f);
        }

        private void OnMqttConnectionClosed(object sender, EventArgs e)
        {
            Debug.LogWarning("CONNECTION LOST!");
            StartCoroutine(DoDisconnect());
            mqttClientConnectionClosed = mqttClientConnected;
            mqttClientConnected = false;
        }

        public void OnMqttConnectionNoAuleClosed()
        {
            Debug.LogWarning("CONNECTION LOST!");
            StartCoroutine(DoDisconnect());
            mqttClientConnectionClosed = mqttClientConnected;
            mqttClientConnected = false;
        }

        private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs msg)
        {
            frontMessageQueue.Add(msg);
        }

        private void SubscribeTopics()
        {
            client.Subscribe(new string[] { brokerTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void UnsubscribeTopics()
        {
            client.Unsubscribe(new string[] { brokerTopic });
        }

        private IEnumerator DoDisconnect()
        {
            yield return new WaitForEndOfFrame();
            CloseConnection();
            Debug.Log("Disconnected.");
        }

        public void CloseConnection()
        {
            mqttClientConnected = false;
            if (client != null)
            {
                if (client.IsConnected)
                {
                    UnsubscribeTopics();
                    client.Disconnect();
                }
                mqttClientConnectionClosed = false;
                client.MqttMsgPublishReceived -= OnMqttMessageReceived;
                client.ConnectionClosed -= OnMqttConnectionClosed;
                client = null;
                StartCoroutine(DoConnect());
            }
        }
    }

}
