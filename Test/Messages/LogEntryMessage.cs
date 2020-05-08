namespace MyMedicalLog
{
    public class LogEntryMessage
    {
        public long entryId;
        public string entryType;
        public long entryLogId;
        public long entityCreatedDate;
        public long entityParentId;
        public long painStartDate;
        public string painDuration;
        public string painSeverity;
        public string painType;
        public string painLocation;
        public int painStrength;
        public string painDescription;
        public string painNotice;
        public string treatmentMedicationUsed;
        public bool doctorViewed;
        public bool doctorUploaded;
        public bool doctorStatus;
    }
}