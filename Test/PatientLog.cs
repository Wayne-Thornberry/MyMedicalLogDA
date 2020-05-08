using System.Collections.Generic;
using Newtonsoft.Json;

namespace MyMedicalLog
{
    public class PatientLog
    {
        public long Id {get;}
        public string Name {get;}
        public long CreatedDate {get;}
        public long UpdatedDate {get;}
        public bool EnableAutoBackup {get;}
        public bool EnableAutoUpload {get;}
        public List<LogEntry> Entries { get; }
 
        public PatientLog(ProfileLogAndEntriesMessage profileLogAndEntriesMessage)
        { 
            Id = profileLogAndEntriesMessage.profileLogMessage.logId;
            Name = profileLogAndEntriesMessage.profileLogMessage.logName;
            CreatedDate = profileLogAndEntriesMessage.profileLogMessage.logCreatedDate;
            UpdatedDate = profileLogAndEntriesMessage.profileLogMessage.logUpdatedDate ;
            EnableAutoBackup = profileLogAndEntriesMessage.profileLogMessage.logEnableAutoBackup ;
            EnableAutoUpload = profileLogAndEntriesMessage.profileLogMessage.logEnableAutoUpload;
            Entries = new List<LogEntry>();
            foreach (var logEntryMessage in profileLogAndEntriesMessage.logEntryMessages)
            {
                Entries.Add(new LogEntry(logEntryMessage));
            }
        }

        [JsonConstructor]
        public PatientLog(long id, string name, long createdDate, long updatedDate, bool enableAutoBackup, bool enableAutoUpload, List<LogEntry> entries)
        {
            Id = id;
            Name = name;
            CreatedDate = createdDate;
            UpdatedDate = updatedDate;
            EnableAutoBackup = enableAutoBackup;
            EnableAutoUpload = enableAutoUpload;
            Entries = entries;
        }

        public ProfileLogAndEntriesMessage GetMessage()
        {
            var entriesMessage = new List<LogEntryMessage>();
            foreach (var logEntry in Entries)
            {
                entriesMessage.Add(logEntry.GetMessage());
            }

            var logMessage = new ProfileLogMessage
            {
                logId = this.Id,
                logName = this.Name,
                logCreatedDate = this.CreatedDate,
                logUpdatedDate = this.UpdatedDate,
                logEnableAutoBackup = this.EnableAutoBackup,
                logEnableAutoUpload = this.EnableAutoUpload
            };

            var logAndEntriesMessage = new ProfileLogAndEntriesMessage
            {
                profileLogMessage = logMessage,
                logEntryMessages = entriesMessage,
            };
            return logAndEntriesMessage;
        }
    }
}