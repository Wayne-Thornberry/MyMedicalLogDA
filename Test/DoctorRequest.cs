namespace Test
{
    public class DoctorRequest
    {
        public int DoctorId;
        public int Passcode;
        public bool ScannedQRCode;
        public UserProfile UserProfile;
    }

    public class UserProfile
    {
        public string FirstName;
        public string LastName;
        public bool IsProfileSetup;
        public bool IsDoctorSetup;
    }
}