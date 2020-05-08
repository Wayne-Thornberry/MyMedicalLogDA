using Newtonsoft.Json;

namespace MyMedicalLog
{
    public class LogEntry
    {
        public long EntryId {get; set;}
        public string EntryType {get; set;}
        public long EntryLogId {get; set;}
        public long EntityCreatedDate {get; set;}
        public long EntityParentId {get; set;}
        public long PainStartDate {get; set;}
        public string PainDuration {get; set;}
        public string PainSeverity {get; set;}
        public string PainType {get; set;}
        public string PainLocation {get; set;}
        public int PainStrength {get; set;}
        public string PainDescription {get; set;}
        public string PainNotice { get; set; }
        public string TreatmentMedicationUsed {get; set;}
        public bool DoctorViewed {get; set;}
        public bool DoctorUploaded {get; set;}
        public bool DoctorStatus {get; set;}
        
        public LogEntry(LogEntryMessage logEntryMessage)
        {
            EntryId = logEntryMessage.entryId;
            EntryType = logEntryMessage.entryType;
            EntryLogId = logEntryMessage.entryLogId;
            EntityCreatedDate = logEntryMessage.entityCreatedDate;
            EntityParentId = logEntryMessage.entityParentId;
            PainStartDate = logEntryMessage.painStartDate;
            PainDuration = logEntryMessage.painDuration;
            PainSeverity = logEntryMessage.painSeverity;
            PainType = logEntryMessage.painType;
            PainLocation = logEntryMessage.painLocation;
            PainStrength = logEntryMessage.painStrength;
            PainDescription = logEntryMessage.painDescription;
            PainNotice = logEntryMessage.painNotice;
            TreatmentMedicationUsed = logEntryMessage.treatmentMedicationUsed;
            DoctorViewed = logEntryMessage.doctorViewed;
            DoctorUploaded = logEntryMessage.doctorUploaded;
            DoctorStatus = logEntryMessage.doctorStatus;
        }


        [JsonConstructor]
        public LogEntry(long entryId, string entryType, long entryLogId, long entityCreatedDate, long entityParentId, long painStartDate, string painDuration, string painSeverity, string painType, string painLocation, int painStrength, string painDescription, string treatmentMedicationUsed, bool doctorViewed, bool doctorUploaded, bool doctorStatus)
        {
            EntryId = entryId;
            EntryType = entryType;
            EntryLogId = entryLogId;
            EntityCreatedDate = entityCreatedDate;
            EntityParentId = entityParentId;
            PainStartDate = painStartDate;
            PainDuration = painDuration;
            PainSeverity = painSeverity;
            PainType = painType;
            PainLocation = painLocation;
            PainStrength = painStrength;
            PainDescription = painDescription;
            TreatmentMedicationUsed = treatmentMedicationUsed;
            DoctorViewed = doctorViewed;
            DoctorUploaded = doctorUploaded;
            DoctorStatus = doctorStatus;
        }

        public LogEntryMessage GetMessage()
        {
            var message = new LogEntryMessage
            {
                entryId = EntryId,
                entryType = EntryType,
                entryLogId = EntryLogId,
                entityCreatedDate = EntityCreatedDate,
                entityParentId = EntityParentId,
                painStartDate = PainStartDate,
                painDuration = PainDuration,
                painSeverity = PainSeverity,
                painType = PainType,
                painLocation = PainLocation,
                painStrength = PainStrength,
                painDescription = PainDescription,
                painNotice = PainNotice,
                treatmentMedicationUsed = TreatmentMedicationUsed,
                doctorViewed = DoctorViewed,
                doctorUploaded = DoctorUploaded,
                doctorStatus = DoctorStatus
            };
            return message;
        }
    }
}