namespace MyMedicalLog
{
    public class ConnectionRequestMessage
    {
        public string uuid;
        public int doctorID;
        public string ppsno;
        public int passcode;
        public bool approved;
        public string reason;
    }
}