using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Customer_Relationship_Management.Repositories.Interfaces;

namespace Customer_Relationship_Management.Repositories.Implements
{
    public class BackupRepository : IBackupRepository
    {
        private readonly string _connectionString;
        private readonly string _backupFolder;

        public BackupRepository(IConfiguration configuration, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new Exception("❌ Không tìm thấy ConnectionString: DefaultConnection");

            _backupFolder = Path.Combine(env.WebRootPath, "backups");

            if (!Directory.Exists(_backupFolder))
                Directory.CreateDirectory(_backupFolder);
        }

        public async Task<string> BackupDatabaseAsync(string backupFolder)
        {
            var dbName = "B2B_CRM";

            var fileName = $"{dbName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            var filePath = Path.Combine(_backupFolder, fileName);

            var sql = $@"BACKUP DATABASE [{dbName}] TO DISK = @path WITH INIT, STATS = 10;";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@path", filePath);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return fileName;
        }

        public async Task<bool> RestoreDatabaseAsync(string backupFilePath)
        {
            var dbName = "B2B_CRM";
            var fullPath = Path.Combine(_backupFolder, backupFilePath);

            if (!File.Exists(fullPath))
                return false;

            // Tạo connection string kết nối tới MASTER
            var masterConnStr = _connectionString
                .Replace("Database=B2B_CRM", "Database=master")
                .Replace("Initial Catalog=B2B_CRM", "Initial Catalog=master");

            var sql = $@"
        ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        RESTORE DATABASE [{dbName}] FROM DISK = @path WITH REPLACE;
        ALTER DATABASE [{dbName}] SET MULTI_USER;
    ";

            using var conn = new SqlConnection(masterConnStr);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@path", fullPath);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return true;
        }


        public async Task<List<string>> GetBackupFilesAsync()
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(_backupFolder))
                    return new List<string>();

                var result = new List<string>();
                var files = Directory.GetFiles(_backupFolder, "*.bak");

                foreach (var file in files)
                    result.Add(Path.GetFileName(file));

                return result;
            });
        }
    }
}
