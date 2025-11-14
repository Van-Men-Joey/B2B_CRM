using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Implementations
{
    public class BackupService : IBackupService
    {
        private readonly IBackupRepository _backupRepository;

        public BackupService(IBackupRepository backupRepository)
        {
            _backupRepository = backupRepository;
        }

        public async Task<string> BackupDatabaseAsync(string backupFolder)
        {
            return await _backupRepository.BackupDatabaseAsync(backupFolder);
        }

        public async Task<bool> RestoreDatabaseAsync(string backupFilePath)
        {
            return await _backupRepository.RestoreDatabaseAsync(backupFilePath);
        }

        public async Task<List<string>> GetBackupFilesAsync()
        {
            return await _backupRepository.GetBackupFilesAsync();
        }
    }
}





