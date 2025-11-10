using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Implements
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IAuditLogService _auditLogService;

        public ContractService(
            IContractRepository contractRepository,
            IAuditLogService auditLogService)
        {
            _contractRepository = contractRepository;
            _auditLogService = auditLogService;
        }

        public async Task<IEnumerable<Contract>> GetByUserAsync(int userId)
        {
            return await _contractRepository.GetByUserAsync(userId);
        }

        public async Task<IEnumerable<Contract>> GetPendingContractsAsync()
        {
            return await _contractRepository.GetPendingContractsAsync();
        }

        public async Task<IEnumerable<Contract>> GetByManagerAsync(int managerId)
        {
            return await _contractRepository.GetByManagerAsync(managerId);
        }

        public async Task<IEnumerable<Contract>> GetByStatusAsync(string status)
        {
            return await _contractRepository.GetByStatusAsync(status);
        }

        public async Task<Contract?> GetByIdAsync(int id)
        {
            return await _contractRepository.GetByIdAsync(id);
        }

        public async Task CreateAsync(Contract contract, int? currentUserId = null)
        {
            await _contractRepository.AddAsync(contract);
            await _contractRepository.SaveChangesAsync();

            // Log create -> newValue = contract
            await _auditLogService.LogAsync(
                userId: currentUserId,
                action: ActionType.Create,
                tableName: "Contracts",
                recordId: contract.ContractID.ToString(),
                oldValue: null,
                newValue: contract
            );
        }

        public async Task UpdateAsync(Contract contract, int? currentUserId = null)
        {
            var existing = await _contractRepository.GetByIdAsync(contract.ContractID);
            if (existing == null) throw new InvalidOperationException("Contract not found");

            var oldValueJson = JsonSerializer.Serialize(existing);
            var oldValue = JsonSerializer.Deserialize<object>(oldValueJson);

            await _contractRepository.UpdateAsync(contract);
            await _contractRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: currentUserId,
                action: ActionType.Update,
                tableName: "Contracts",
                recordId: contract.ContractID.ToString(),
                oldValue: oldValue,
                newValue: contract
            );
        }

        public async Task DeleteAsync(int id, int? currentUserId = null)
        {
            var existing = await _contractRepository.GetByIdAsync(id);
            if (existing == null) throw new InvalidOperationException("Contract not found");

            var oldValueJson = JsonSerializer.Serialize(existing);
            var oldValue = JsonSerializer.Deserialize<object>(oldValueJson);

            existing.IsDeleted = true;
            existing.UpdatedAt = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(existing);
            await _contractRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(
                userId: currentUserId,
                action: ActionType.Delete,
                tableName: "Contracts",
                recordId: existing.ContractID.ToString(),
                oldValue: oldValue,
                newValue: null
            );
        }

        // Updated: ApproveAsync now sets ApprovedByUserID and ApprovedAt, then logs the update
        public async Task ApproveAsync(int id, string newStatus, int? currentUserId = null)
        {
            var existing = await _contractRepository.GetByIdAsync(id);
            if (existing == null) throw new InvalidOperationException("Contract not found");

            // Snapshot old
            var oldValueJson = JsonSerializer.Serialize(existing);
            var oldValue = JsonSerializer.Deserialize<object>(oldValueJson);

            // Update approval info
            existing.ApprovalStatus = newStatus;
            existing.ApprovedAt = DateTime.UtcNow;
            existing.ApprovedByUserID = currentUserId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(existing);
            await _contractRepository.SaveChangesAsync();

            // Snapshot new (we only need relevant fields but we can snapshot whole entity)
            var newValueJson = JsonSerializer.Serialize(new { existing.ContractID, existing.ApprovalStatus, existing.ApprovedAt, existing.ApprovedByUserID });
            var newValue = JsonSerializer.Deserialize<object>(newValueJson);

            await _auditLogService.LogAsync(
                userId: currentUserId,
                action: ActionType.Update,
                tableName: "Contracts",
                recordId: existing.ContractID.ToString(),
                oldValue: oldValue,
                newValue: newValue
            );
        }
    }
}