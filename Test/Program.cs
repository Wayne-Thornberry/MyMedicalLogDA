using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyMedicalLog
{ 
    internal class Program
    {
        private static int DOCTOR_ID = 25565;
        
        private delegate void CommandEventHandler(params string[] args);
        private static Dictionary<string, CommandEventHandler> _commands;  
        private static List<DoctorProfile> _doctorProfiles;

        private static PatientProfile _selectedPatientProfile;
        private static DoctorProfile _selectedDoctorProfile;
        private static PatientLog _selectedPatientLog;
        private static RabbitMQService _rabbitMqService;
        
        
        private static bool _enableDebug = true;
        private static Dictionary<string, ConnectionRequest> _connectionRequests;
        private static List<ConnectionRequestMessage> _connectionRequestMessages;

        public static void Main(string[] args)
        {
            _rabbitMqService = new RabbitMQService("test", "test", "/", "109.78.62.161");
            
            LoadData();
            LoadCommands();
            LoadRequests();
            
            if (!StartLogin()) return;
            Console.WriteLine("Welcome: " + _selectedDoctorProfile.Name);
            _rabbitMqService.StartReceiving("DOCTOR-ID:" + _selectedDoctorProfile.Id + "-QUEUE");
            _rabbitMqService.AddMessageEventHandler("USER_CONNECTION_REQUEST", OnUserConnectionRequest);
            _rabbitMqService.AddMessageEventHandler("LOG_UPLOAD", OnLogUpload);
            _rabbitMqService.AddMessageEventHandler("LOG_AND_ENTRIES", OnLogEntryUpload);
            _rabbitMqService.AddMessageEventHandler("DOCTOR_SYNC", OnDoctorSync); 
            _selectedDoctorProfile.LoadRequest(_connectionRequests);
            Run();
        }

        private static void OnDoctorSync(string uuid, string message)
        {
            var doctorSyncMessage = JsonConvert.DeserializeObject<DoctorSyncMessage>(message);
            var  patient = GetPatient(doctorSyncMessage.uuid);
            if (patient == null) return;
            var queue = patient.GetQueue(doctorSyncMessage.uuid);
            _rabbitMqService.SendMessage("PATIENT_DETAILS", patient.GetDetailsMessage(), queue);
            _rabbitMqService.SendMessage("DOCTOR_DETAILS", _selectedDoctorProfile.GetDetailsMessage(), queue);
        }

        private static void OnLogEntryUpload(string uuid, string message)
        {
            var patientProfile = GetPatient(uuid);
            var profileLogAndEntriesMessage = JsonConvert.DeserializeObject<ProfileLogAndEntriesMessage>(message);
            var log = patientProfile.GetLog(profileLogAndEntriesMessage.profileLogMessage.logName);
            if (log == null)
            {
                patientProfile.Logs.Add(new PatientLog(profileLogAndEntriesMessage));
            }
            else
            {
                foreach (var logEntryMessage in profileLogAndEntriesMessage.logEntryMessages)
                {
                    log.Entries.Add(new LogEntry(logEntryMessage));
                }
            }
            SaveData();
        }

        private static void LoadRequests()
        {
            _connectionRequests = new Dictionary<string, ConnectionRequest>();
            if (!File.Exists("requests.txt"))
                File.WriteAllText("requests.txt", JsonConvert.SerializeObject(_connectionRequests));
            _connectionRequestMessages = JsonConvert.DeserializeObject<List<ConnectionRequestMessage>>(File.ReadAllText("requests.txt"));
            foreach (var connectionRequestMessage in _connectionRequestMessages)
                _connectionRequests.Add(connectionRequestMessage.uuid, new ConnectionRequest(_rabbitMqService, connectionRequestMessage));
        }


        private static void OnLogUpload(string uuid, string message)
        {
            
        }

        private static void OnUserConnectionRequest(string uuid, string message)
        {
            var connectionRequestMessage = JsonConvert.DeserializeObject<ConnectionRequestMessage>(message);
            if (_connectionRequests.ContainsKey(connectionRequestMessage.uuid)) return;
            if (connectionRequestMessage.passcode != _selectedDoctorProfile.PassCode) return;
            var connectionRequest = new ConnectionRequest(_rabbitMqService, connectionRequestMessage);
            _selectedDoctorProfile.AddConnectionRequest(connectionRequestMessage.uuid, connectionRequest);
            _connectionRequests.Add(connectionRequestMessage.uuid, connectionRequest);
            _connectionRequestMessages.Add(connectionRequestMessage);
            File.WriteAllText("requests.txt", JsonConvert.SerializeObject(_connectionRequestMessages));

        }

        private static void LoadData()
        {
            if (!File.Exists("data.txt"))
            {
                _doctorProfiles = GenerateDoctors(20);
                File.WriteAllText("data.txt", JsonConvert.SerializeObject(_doctorProfiles));
            }
            else
            {
                _doctorProfiles = JsonConvert.DeserializeObject<List<DoctorProfile>>(File.ReadAllText("data.txt"));
            }

        }

        private static void LoadCommands()
        {
            _commands = new Dictionary<string, CommandEventHandler>();
            RegisterCommand("Send", Send);
            RegisterCommand("View", View);
            RegisterCommand("Set", Set);
            RegisterCommand("Generate", Generate);
            RegisterCommand("Select", Select);
            RegisterCommand("Request", Request);
            RegisterCommand("Attention", Attention);
        }

        private static void Attention(string[] args)
        {
            try
            {
                if (args.Length == 1)
                {
                    var id = int.Parse(args[0]);
                    if (_selectedPatientLog == null) return;
                    if (id >= _selectedPatientLog.Entries.Count)
                        id = _selectedPatientLog.Entries.Count - 1;
                    _selectedPatientLog.Entries[id].DoctorStatus = true;
                    var ds = _selectedPatientLog.GetMessage();
                    var data = JsonConvert.SerializeObject(ds);
                    foreach (var uuid in _selectedPatientProfile.UUIDs)
                    {
                        var queue = _selectedPatientProfile.GetQueue(uuid);
                        _rabbitMqService.SendMessage("LOG_AND_ENTRIES", data, queue);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void Request(string[] args)
        {
            if (args.Length < 2) return;
            if(_selectedDoctorProfile == null) Console.WriteLine("Please select a doctor profile to accept/decline request");
            var type = args[0];
            var id = int.Parse(args[1]);
            var connectionRequest = _selectedDoctorProfile.GetRequest(id);
            Console.Write(JsonConvert.SerializeObject(connectionRequest, Formatting.Indented));
            if (type.Equals("accept") || type.Equals("reject"))
            {
                if (type.Equals("accept"))
                {
                    var patientProfile = GetPatient(connectionRequest.PPSNo);
                    if (patientProfile == null)
                    {
                        Console.WriteLine("An account could not be found with the requested ppsno: " + connectionRequest.PPSNo);
                        Console.Write("Do you wish to create an account now? Y | N");
                        var result = Console.ReadLine();
                        if(string.IsNullOrEmpty(result))
                            result = "N"; 
                        patientProfile = result.Equals("Y") ? CreateNewPatientProfile() : CreateNewPatientProfile(connectionRequest.PPSNo, _selectedDoctorProfile.Id);
                        _selectedDoctorProfile.PatientProfiles.Add(patientProfile);
                    }
                    patientProfile.AddConnection(connectionRequest.UUID);
                    var queue = patientProfile.GetQueue(connectionRequest.UUID);
                    _rabbitMqService.SendMessage("PATIENT_DETAILS", patientProfile.GetDetailsMessage(), queue);
                    _rabbitMqService.SendMessage("DOCTOR_DETAILS", _selectedDoctorProfile.GetDetailsMessage(), queue);
                    connectionRequest.Approve();
                    SaveData();
                }
                else if (type.Equals("reject"))
                {
                    var reason = "";
                    if (args.Length == 2)
                    {
                        reason = args[0];
                    }
                    connectionRequest.Reject(reason);
                }
                _connectionRequests.Remove(connectionRequest.UUID);
                _connectionRequestMessages.Remove(connectionRequest.Message);
                File.WriteAllText("requests.txt", JsonConvert.SerializeObject(_connectionRequestMessages));
                _selectedDoctorProfile.RemoveConnectionRequest(connectionRequest.UUID);
                
            }
        }

        private static PatientProfile GetPatient(string key)
        {
            foreach (var patientProfile in _selectedDoctorProfile.PatientProfiles)
            {
                if (patientProfile.HasUUID(key)|| patientProfile.PPSNo.Equals(key))
                    return patientProfile;
            }
            return null;
        }

        private static void SaveData()
        {
            File.WriteAllText("data.txt", JsonConvert.SerializeObject(_doctorProfiles));
        } 

        private static PatientProfile CreateNewPatientProfile(string ppsno = "", int id = 0)
        {
            var patientProfile = new PatientProfile();
            var input = "";
            while (string.IsNullOrEmpty(input))
            {
                Console.Write("Please enter the full name for this account: ");
                input = Console.ReadLine();
            }

            var name = input;
            input = "";
            
            
            int age = 0;
            while (!int.TryParse(input, out age))
            {
                Console.Write("Please enter the age for this account: ");
                input = Console.ReadLine();
            }
            patientProfile.UUIDs = new List<string>();
            patientProfile.Logs = new List<PatientLog>();
            patientProfile.Name = input;
            patientProfile.PPSNo = ppsno;
            patientProfile.DoctorId = id;
            patientProfile.Age = age;
            return patientProfile;
        }

        private static void Generate(string[] args)
        {
            if (args.Length != 2) return;
            var amount = int.Parse(args[1]);
            if (args[0].Equals("Patients"))
            {
                if (_doctorProfiles.Count == 0) return;
                if(!File.Exists("patients.txt"))
                    _selectedDoctorProfile.PatientProfiles = GeneratePatients(_selectedDoctorProfile, amount);
                SaveData();
            }else if (args[0].Equals("Doctors"))
            {
                if(!File.Exists("doctors.txt"))
                    _doctorProfiles = GenerateDoctors(amount);
                SaveData();
            }
        }

        private static List<DoctorProfile> GenerateDoctors(int amount)
        {
            var random = new Random(); 
            var mFirstNames = File.ReadAllText("mfirstnames.txt").Split('\n');
            var fFirstNames = File.ReadAllText("ffirstnames.txt").Split('\n');
            var firstNames = File.ReadAllText("ofirstnames.txt").Split('\n');
            var lastNames = File.ReadAllText("lastnames.txt").Split('\n');
            var genders = new[] {"Male", "Female", "Other"};
            var pssnoL = new[] {'O', 'M'};
            var doctorProfiles = new List<DoctorProfile>();

            for (int i = 0; i < amount; i++)
            {
                var gender = genders[random.Next(genders.Length)];
                var firstName = "";
                if (gender.Equals("Male"))
                {
                    firstName = mFirstNames[random.Next(mFirstNames.Length)];
                }
                else if (gender.Equals("Female"))
                {
                    firstName = fFirstNames[random.Next(fFirstNames.Length)];
                }
                else
                {
                    firstName = firstNames[random.Next(firstNames.Length)];
                }

                var name = $"{firstName} {lastNames[random.Next(lastNames.Length)]}";
                var age = random.Next(30,75);
                var ppsno = $"{(int)(random.NextDouble() * 10000000)}{pssnoL[random.Next(pssnoL.Length)]}";
                var id = random.Next(1000,2000);
                var passcode =  random.Next(10000,99999);
                doctorProfiles.Add(new DoctorProfile {Id = id, Name = name, Gender = gender, Age = age, PPSNo = ppsno, PassCode = passcode});
            }
            Console.Write("");
            doctorProfiles.Add(new DoctorProfile {Id = -1, Name = "Gregory House", Gender = "Male", Age = 45, PPSNo = "0000000A", PassCode = 0});
            foreach (var doctorProfile in doctorProfiles)
            {
                doctorProfile.PatientProfiles = GeneratePatients(doctorProfile, random.Next(10));
                if (doctorProfile.Id != -1) continue;
                var patientProfile = new PatientProfile
                {
                    DoctorId = -1,
                    Name = "Wayne Thornberry",
                    Gender = "Male",
                    Age = 23,
                    PPSNo = "8691409O",
                    UUIDs = new List<string>(),
                    Logs = new List<PatientLog>()
                };
                doctorProfile.PatientProfiles.Add(patientProfile);
            }
            return doctorProfiles;
        }

        private static void Select(string[] args)
        {
            if (args.Length != 2) return;
            var target = args[0];
            var id = int.Parse(args[1]); 
            if (target.Equals("patient"))
            {
                _selectedPatientProfile = _selectedDoctorProfile.GetPatient(id); 
            }else if (target.Equals("log"))
            {
                if (_selectedPatientProfile == null)
                {
                    Console.WriteLine("Please select a patient profile first");
                    return;
                }
                _selectedPatientLog = _selectedPatientProfile.GetLog(id);
            }
        }

        private static List<PatientProfile> GeneratePatients(DoctorProfile doctorProfile, int amount)
        {
            var random = new Random(); 
            var mFirstNames = File.ReadAllText("mfirstnames.txt").Split('\n');
            var fFirstNames = File.ReadAllText("ffirstnames.txt").Split('\n');
            var firstNames = File.ReadAllText("ofirstnames.txt").Split('\n');
            var lastNames = File.ReadAllText("lastnames.txt").Split('\n');
            var genders = new[] {"Male", "Female", "Other"};
            var pssnoL = new[] {'O', 'M'};
            var patientProfiles = new List<PatientProfile>();
            
            for (int i = 0; i < amount; i++)
            {
                var gender = genders[random.Next(genders.Length)];
                var firstName = "";
                if (gender.Equals("Male"))
                {
                    firstName = mFirstNames[random.Next(mFirstNames.Length)];
                }
                else if (gender.Equals("Female"))
                {
                    firstName = fFirstNames[random.Next(fFirstNames.Length)];
                }
                else
                {
                    firstName = firstNames[random.Next(firstNames.Length)];
                }

                var name = $"{firstName} {lastNames[random.Next(lastNames.Length)]}";
                var age = random.Next(75);
                var ppsno = $"{(int)(random.NextDouble() * 10000000)}{pssnoL[random.Next(pssnoL.Length)]}";
                var patientProfile = new PatientProfile
                {
                    DoctorId = doctorProfile.Id,
                    Name = name,
                    Gender = gender,
                    Age = age,
                    PPSNo = ppsno,
                    UUIDs = new List<string>(),
                    Logs = new List<PatientLog>()
                };
                patientProfiles.Add(patientProfile);
            } 
            return patientProfiles;
        }

        private static void Set(string[] args)
        {
            if (args[0].Equals("passcode"))
            {
                if (_selectedPatientProfile == null) return;
                try
                {
                    var passcode = int.Parse(args[1]);
                    _selectedPatientProfile.Passcode  = passcode;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }else if (args[0].Equals("status"))
            {
                var statusMessage = new DoctorStatusMessage();
                if (args[1].Equals("online"))
                {
                    statusMessage.status = "online";
                    statusMessage.lastonline = DateTime.Now.Ticks;
                }
                else
                {
                    statusMessage.status = "offline";
                }

                foreach (var patientProfile in _selectedDoctorProfile.PatientProfiles)
                {
                    foreach (var uuids in patientProfile.UUIDs)
                    {
                        var queue = patientProfile.GetQueue(uuids);
                        _rabbitMqService.SendMessage("DOCTOR_STATUS",JsonConvert.SerializeObject(statusMessage), queue);
                    }
                }
            }
        }

        private static void Run()
        {
            while (true)
            {
                var text = Console.ReadLine();
                if (!string.IsNullOrEmpty(text))
                {
                    var args = new List<string>(text.Split(' '));
                    var command = args[0];
                    args.Remove(command);
                    RunCommand(command, args.ToArray());
                }
            }
        }


        private static bool StartLogin()
        {
            if (_enableDebug)
            {
                foreach (var doctorProfile in _doctorProfiles)
                {
                    if (doctorProfile.Id != -1) continue;
                    _selectedDoctorProfile = doctorProfile;
                    break;
                }
                return true;
            }

            var sid = "";
            var doctorId = 0;
            while (string.IsNullOrEmpty(sid) || !int.TryParse(sid, out doctorId))
            {
                Console.WriteLine("Enter Doctor ID:");
                sid = Console.ReadLine();
            }

            foreach (var doctorProfile in _doctorProfiles)
            {
                if (doctorProfile.Id != doctorId) continue;
                Console.WriteLine("Enter Doctor Passcode:");
                var spasscode = Console.ReadLine();
                var passcode = 0;
                if (int.TryParse(spasscode, out passcode))
                {
                    _selectedDoctorProfile = doctorProfile;
                    return true;
                }
                break;
            }
            Console.WriteLine("No doctor found with id: " + doctorId);
            return false;
        }




        private static void View(string[] args)
        {
            if (args.Length != 2) return;
            var target = args[0];
            var id = int.Parse(args[1]); 
            if (target.Equals("patient"))
            {
                var patientProfile = _selectedDoctorProfile.GetPatient(id);
                Console.Write(JsonConvert.SerializeObject(patientProfile, Formatting.Indented));
            }else if (target.Equals("log"))
            {
                if(_selectedPatientProfile == null) {
                    Console.WriteLine("Please select a patient first to select a log");
                    return;
                }
                var log = _selectedPatientProfile.GetLog(id);
                Console.Write(JsonConvert.SerializeObject(log, Formatting.Indented));
            }
        }


        private static void Send(string[] args)
        {
            //int USER_ID = 202;
            //if (!int.TryParse(args[1], out USER_ID)) return;
            //Send(USER_ID, args[2]); 
        }


        private static void RegisterCommand(string command, CommandEventHandler handler)
        {
            Console.WriteLine("Registering Command: {0}", command.ToLower());
            _commands.Add(command.ToLower(), handler);
        } 
        
        private static void RunCommand(string command, params string[] args)
        {
            try
            {
                _commands[command.ToLower()].Invoke(args);
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                Console.WriteLine("No event found with command: {0}",command);
            }
        }
    }
}