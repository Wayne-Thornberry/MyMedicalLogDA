using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Test
{ 
    internal class Program
    {
        private static int DOCTOR_ID = 25565;
        
        public delegate void MyEventHandler(params string[] args);
        private static Dictionary<string, MyEventHandler> Commands;
        private static Dictionary<long, LogEntry> _log;

        public static void Main(string[] args)
        {
            Commands = new Dictionary<string, MyEventHandler>();
            _log = new Dictionary<long, LogEntry>();
            RegisterCommand("Wow", Testing);
            RegisterCommand("Send", Send);
            RegisterCommand("View", View);
            string sid = "";
            while (string.IsNullOrEmpty(sid) || !int.TryParse(sid, out DOCTOR_ID))
            {
                Console.WriteLine("Enter Doctor ID:");
                sid = Console.ReadLine();
            }
            
            Factory = new ConnectionFactory();
            Factory.UserName = "test";
            Factory.Password = "test";
            Factory.VirtualHost = "/";
            Factory.HostName = "64.43.3.207";
            Connection = Factory.CreateConnection();
            Channel = Connection.CreateModel();
            Consumer = new EventingBasicConsumer(Channel);
            Consumer.Received += Received;
            string queue = "DOCTOR-ID:" + DOCTOR_ID + "-QUEUE";
            Channel.QueueDeclare(queue, false,false,false,null);
            Channel.BasicConsume(queue, true, Consumer);
            
            while (true)
            {
                var command = Console.ReadLine();
                if (!string.IsNullOrEmpty(command))
                {
                    var words = command.Split(' ');
                    RunCommand(words[0], words);
                }
            }
        }


        private static void Received(object sender, BasicDeliverEventArgs e)
        {
            var body = e.Body;
            var message = Encoding.UTF8.GetString(body);
            var type = Encoding.UTF8.GetString((byte[]) e.BasicProperties.Headers["TYPE"]);
            Console.WriteLine("{0} Received {1}", type, message);
            if (type.Equals("LOG_UPLOAD", StringComparison.CurrentCultureIgnoreCase))
            {
                var entries = JsonConvert.DeserializeObject<List<LogEntry>>(message);
                foreach (var entry in entries)
                {
                    if (!_log.ContainsKey(entry.entryId))
                    {
                        _log.Add(entry.entryId, entry);
                    }
                }
            }
            else
            {
                    
            }
        }

        public static ConnectionFactory Factory { get; set; }
        public static EventingBasicConsumer Consumer { get; set; }
        public static IModel Channel { get; set; }
        public static IConnection Connection { get; set; }

        private static void View(string[] args)
        {
            int id;
            if (int.TryParse(args[1], out id))
            {
                Console.WriteLine(JsonConvert.SerializeObject(_log[id], Formatting.Indented));
            }
        }


        private static void Send(string[] args)
        {
            int USER_ID = 202;
            if (!int.TryParse(args[1], out USER_ID)) return;
            Send(USER_ID, args[2]); 
        }

        private static void Send(int id, string message)
        { 
            string queue = "USER-ID:" + id + "-QUEUE";
            var t = Channel.CreateBasicProperties();
            t.Type = "RANDOM_MESSAGE";
            t.Headers = new Dictionary<string, object>
            {
                {"TEST","TESTING"}
            };
            Channel.QueueDeclare(queue,
                durable: false,
                exclusive: false,
                autoDelete: true,
                arguments: null);
                    
            Channel.BasicPublish("",queue, t, Encoding.ASCII.GetBytes(message));
        }

        private static void RegisterCommand(string command, MyEventHandler handler)
        {
            Console.WriteLine("Registering Command: {0}", command.ToLower());
            Commands.Add(command.ToLower(), handler);
        }

        private static void Testing(string[] args)
        {
            Console.Write("Wow, the command triggered ");
            foreach (var arg in args)
            {
                Console.Write(arg);
            }
            Console.Write("\n");
        }
        
        private static void RunCommand(string command, params string[] args)
        {
            try
            {
                Commands[command.ToLower()].Invoke(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("No event found with command: {0}",command);
            }
        }
    }

    internal class LogEntry
    {
        public long entryId;
        public long logId;
        public string recordedDate;
        public string estStartDate;
        public string painDuration;
        public string painSeverity;
        public string painType;
        public string painLocation;
        public string painExperience;
        public string fullDescription;
        public string medicationUsed;
    }
}