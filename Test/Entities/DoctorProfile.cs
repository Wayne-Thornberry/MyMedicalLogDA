using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MyMedicalLog
{
    internal class DoctorProfile
    {
        public int Id { get; set; }
        public string Name {get; set;}
        public string Gender {get; set;}
        public int Age {get; set;}
        public int PassCode { get; set; }
        public string PPSNo { get; set; }
        public List<PatientProfile> PatientProfiles { get; set; }
        [JsonIgnore] private Dictionary<string, ConnectionRequest> _connectionRequests;


        public string GetDetailsMessage()
        {
            var ddM = new DoctorDetailsMessage()
            {
                id = this.Id,
                name = Name,
                gender = Gender,
                age = Age
            };
            return JsonConvert.SerializeObject(ddM);
        } 

        public PatientProfile GetPatient(int id)
        {
            try
            {
                var patientProfile = PatientProfiles[id];
                return patientProfile;
            }
            catch (Exception e)
            {
                return PatientProfiles[PatientProfiles.Count - 1];
            }
        }

        public void LoadRequest(Dictionary<string, ConnectionRequest> connectionRequests)
        {
            _connectionRequests = new Dictionary<string, ConnectionRequest>();
            foreach (var connectionRequest in connectionRequests)
            {
                if (connectionRequest.Value.DoctorID == this.Id)
                    _connectionRequests.Add(connectionRequest.Key, connectionRequest.Value);
            }
            Console.WriteLine($"{_connectionRequests.Count} Connection Requests");
        }

        public void AddConnectionRequest(string uuid, ConnectionRequest connectionRequest)
        {
            _connectionRequests.Add(uuid, connectionRequest); 
            Console.WriteLine($"{_connectionRequests.Count} Connection Requests");
        }

        public ConnectionRequest GetRequest(int id)
        {
            if (id >= _connectionRequests.Values.Count)
                id = _connectionRequests.Values.Count - 1;
            return _connectionRequests.Values.ToArray()[id];
        }

        public void RemoveConnectionRequest(string uuid)
        {
            _connectionRequests.Remove(uuid); 
            Console.WriteLine($"{_connectionRequests.Count} Connection Requests");
        }
    }
}