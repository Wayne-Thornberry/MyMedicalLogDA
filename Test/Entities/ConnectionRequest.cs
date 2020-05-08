using Newtonsoft.Json;

namespace MyMedicalLog
{
    public class ConnectionRequest
    {
       public string UUID {get;}
       public int DoctorID {get;}
       public string PPSNo {get;}
       public int Passcode {get;}
       [JsonIgnore]
       public ConnectionRequestMessage Message { get; set; }

       [JsonIgnore]
        private RabbitMQService _service;

        public ConnectionRequest(RabbitMQService service, ConnectionRequestMessage message)
        {
            this._service = service;
            this.Message = message;
            this.DoctorID = message.doctorID;
            this.UUID = message.uuid;
            this.PPSNo = message.ppsno;
            this.Passcode = message.passcode;
        }
        
        public void Approve()
        {
            Message.approved = true;
            var message = JsonConvert.SerializeObject(Message);
            var queue = "USER-ID:" + Message.uuid + "-QUEUE";
            _service.SendMessage("USER_CONNECTION_REQUEST", message,queue);
        }
        
        public void Reject(string reason)
        {
            Message.reason = reason;
            var message = JsonConvert.SerializeObject(Message);
            var queue = "USER-ID:" + Message.uuid + "-QUEUE";
            _service.SendMessage("USER_CONNECTION_REQUEST", message, queue);
        }
    }
}