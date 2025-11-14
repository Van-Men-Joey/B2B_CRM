using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Repositories.Interfaces
{
    public interface IBackupRepository
    {
        Task<string> BackupDatabaseAsync(string backupFolder);
        Task<bool> RestoreDatabaseAsync(string backupFilePath);
        Task<List<string>> GetBackupFilesAsync();
    }
}
