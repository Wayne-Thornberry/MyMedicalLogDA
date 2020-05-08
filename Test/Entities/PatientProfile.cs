using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MyMedicalLog
{
    internal class PatientProfile
    {
        public string Name {get; set;}
        public string Gender {get; set;}
        public int Age {get; set;}
        public int Passcode { get; set; }
        public string PPSNo { get; set; }
        public int DoctorId { get; set; }
        public List<string> UUIDs { get; set; }
        public List<PatientLog> Logs { get; set; }



        public string GetDetailsMessage()
        {
            var ppM = new PatientDetailsMessage()
            {
                name = Name,
                gender = Gender,
                age = Age,
                ppsno = PPSNo,
            };
            return JsonConvert.SerializeObject(ppM);
        }

        public void AddConnection(string uuid)
        {
            if(UUIDs ==null)
                UUIDs = new List<string>();
            UUIDs.Add(uuid);
        }

        public string GetQueue(string uuid)
        {
            return "USER-ID:" + uuid + "-QUEUE";
        }

        public bool HasUUID(string key)
        {
            return UUIDs != null && UUIDs.Contains(key);
        }

        public PatientLog GetLog(int id)
        {
            foreach (var log in Logs)
            {
                if (log.Id == id)
                    return log;
            }
            return null;
        }
        
        public PatientLog GetLog(string logName)
        {
            foreach (var log in Logs)
            {
                if (log.Name.Equals(logName, StringComparison.OrdinalIgnoreCase))
                    return log;
            }
            return null;
        }
    }
}