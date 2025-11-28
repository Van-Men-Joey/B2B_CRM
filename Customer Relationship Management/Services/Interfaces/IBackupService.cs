using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Interfaces
{
    public interface IBackupService
    {
        Task<string> BackupDatabaseAsync(string backupFolder);
        Task<bool> RestoreDatabaseAsync(string backupFilePath);
        Task<List<string>> GetBackupFilesAsync();
    }
}
