using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyMedicalLog
{
    public class RabbitMQService
    {
        private IModel _channel;

        public delegate void MessageEventHandler(string uuid, string message);
        private static Dictionary<string, MessageEventHandler> _eventHandlers;

        public RabbitMQService(string username, string password, string virtualHost, string hostName)
        {
            _channel = CreateChannel(username, password, virtualHost, hostName);
            _eventHandlers = new Dictionary<string, MessageEventHandler>();
        }

        public void StartReceiving(string queue)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += MessageReceived; 
            _channel.QueueDeclare(queue, false, false, false, null);
            _channel.BasicConsume(queue, true, consumer); 
        }
        
        public IModel CreateChannel(string username, string password, string virtualHost, string hostName)
        {
            var connectionFactory = new ConnectionFactory();
            connectionFactory.UserName = username;
            connectionFactory.Password = password;
            connectionFactory.VirtualHost = virtualHost;
            connectionFactory.HostName = hostName;
            var connection = connectionFactory.CreateConnection();
            return connection.CreateModel();
        }

        public void AddMessageEventHandler(string key, MessageEventHandler handler )
        {
            _eventHandlers.Add(key, handler);
        }
        
        public void RemoveMessageEventHandler(string key)
        {
            _eventHandlers.Remove(key);
        }

        private void MessageReceived(object sender, BasicDeliverEventArgs e)
        {
            var body = e.Body;
            var message = Encoding.UTF8.GetString(body);
            var type = Encoding.UTF8.GetString((byte[]) e.BasicProperties.Headers["TYPE"]);
            var uuid = Encoding.UTF8.GetString((byte[]) e.BasicProperties.Headers["UUID"]);
            Console.WriteLine("{0} {1} Received {2}",uuid, type, message);
            _eventHandlers[type].Invoke(uuid, message);
        }
        
        public void SendMessage(string type, string message, string queue)
        { 
            var properties = _channel.CreateBasicProperties();
            properties.Type = type;
            properties.Headers = new Dictionary<string, object>
            {
                {"TYPE",type}
            };
            _channel.QueueDeclare(queue, false, false, true, null);
            _channel.BasicPublish("", queue, properties, Encoding.ASCII.GetBytes(message));
        }
    }

}