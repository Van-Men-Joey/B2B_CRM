using Customer_Relationship_Management.Hubs; // Nhớ dòng này
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.Repositories.Interfaces;
using Customer_Relationship_Management.Services.Interfaces;
using Customer_Relationship_Management.ViewModels; // Nhớ dòng này cho WebhookDto
using Microsoft.AspNetCore.SignalR; // Nhớ dòng này
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Customer_Relationship_Management.Services.Implements
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IAuditLogService _auditLogService;

        // --- KHAI BÁO BIẾN _hubContext Ở ĐÂY ---
        private readonly IHubContext<PaymentHub> _hubContext;

        public ContractService(
            IContractRepository contractRepository,
            IAuditLogService auditLogService,
            IHubContext<PaymentHub> hubContext) // Inject vào Constructor
        {
            _contractRepository = contractRepository;
            _auditLogService = auditLogService;
            _hubContext = hubContext; // Gán giá trị
        }

        // ... Các hàm Get (Giữ nguyên) ...
        public async Task<IEnumerable<Contract>> GetByUserAsync(int userId) => await _contractRepository.GetByUserAsync(userId);
        public async Task<IEnumerable<Contract>> GetPendingContractsAsync() => await _contractRepository.GetPendingContractsAsync();
        public async Task<IEnumerable<Contract>> GetByManagerAsync(int managerId) => await _contractRepository.GetByManagerAsync(managerId);
        public async Task<IEnumerable<Contract>> GetByStatusAsync(string status) => await _contractRepository.GetByStatusAsync(status);
        public async Task<Contract?> GetByIdAsync(int id) => await _contractRepository.GetByIdAsync(id);

        // ... Create, Update, Delete (Giữ nguyên) ...
        public async Task CreateAsync(Contract contract, int? currentUserId = null)
        {
            await _contractRepository.AddAsync(contract);
            await _contractRepository.SaveChangesAsync();
            await _auditLogService.LogAsync(currentUserId, ActionType.Create, "Contracts", contract.ContractID.ToString(), null, contract);
        }

        public async Task UpdateAsync(Contract contract, int? currentUserId = null)
        {
            var existing = await _contractRepository.GetByIdAsync(contract.ContractID);
            if (existing == null) throw new InvalidOperationException("Contract not found");

            var oldValue = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(existing));

            await _contractRepository.UpdateAsync(contract);
            await _contractRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(currentUserId, ActionType.Update, "Contracts", contract.ContractID.ToString(), oldValue, contract);
        }

        public async Task DeleteAsync(int id, int? currentUserId = null)
        {
            var existing = await _contractRepository.GetByIdAsync(id);
            if (existing == null) return;

            var oldValue = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(existing));
            existing.IsDeleted = true;
            await _contractRepository.UpdateAsync(existing);
            await _contractRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(currentUserId, ActionType.Delete, "Contracts", existing.ContractID.ToString(), oldValue, null);
        }

        // --- MANAGER DUYỆT (Bước 1) ---
        public async Task ApproveAsync(int id, string newStatus, int? currentUserId = null)
        {
            var existing = await _contractRepository.GetByIdAsync(id);
            if (existing == null) throw new InvalidOperationException("Contract not found");

            var oldValue = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(existing));

            existing.ApprovalStatus = newStatus;
            existing.ApprovedAt = DateTime.UtcNow;
            existing.ApprovedByUserID = currentUserId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(existing);
            await _contractRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(currentUserId, ActionType.Update, "Contracts", existing.ContractID.ToString(), oldValue, new { Status = newStatus });
        }

        // --- DIRECTOR KÝ DUYỆT (Bước 2) ---
        public async Task DirectorSignAsync(int id, int directorId)
        {
            var existing = await _contractRepository.GetByIdAsync(id);
            if (existing == null) throw new InvalidOperationException("Contract not found");

            if (existing.ApprovalStatus != "ManagerApproved")
            {
                throw new InvalidOperationException("Hợp đồng chưa được Manager thông qua hoặc đã được xử lý.");
            }

            var oldValue = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(existing));

            existing.ApprovalStatus = "Approved";
            existing.DirectorApprovedByUserID = directorId;
            existing.DirectorApprovedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(existing);
            await _contractRepository.SaveChangesAsync();

            await _auditLogService.LogAsync(directorId, ActionType.Update, "Contracts", existing.ContractID.ToString(), oldValue, new { Status = "Approved (Signed by Director)" });
        }

        // --- XỬ LÝ THANH TOÁN TỰ ĐỘNG (WEBHOOK) ---
        public async Task ProcessPaymentWebhookAsync(WebhookDto data)
        {
            // 1. Phân tích nội dung CK để lấy ID. Ví dụ: "THANH TOAN HOP DONG #123"
            var match = Regex.Match(data.Content, @"#(\d+)");
            if (!match.Success)
            {
                match = Regex.Match(data.Content, @"CTR(\d+)", RegexOptions.IgnoreCase);
            }

            if (!match.Success) return;

            if (!int.TryParse(match.Groups[1].Value, out int contractId)) return;

            // 2. Lấy hợp đồng
            var contract = await _contractRepository.GetByIdAsync(contractId);
            if (contract == null) return;

            // 3. Update trạng thái
            if (contract.PaymentStatus == "Paid") return;

            var oldValue = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(contract));

            contract.PaymentStatus = "Paid";
            contract.PaymentAt = DateTime.UtcNow;
            contract.UpdatedAt = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(contract);
            await _contractRepository.SaveChangesAsync();

            // 4. Audit
            await _auditLogService.LogAsync(null, ActionType.Update, "Contracts", contractId.ToString(), oldValue, new { Payment = "Auto via Webhook", Amount = data.Amount });

            // 5. Bắn SignalR (Dùng biến _hubContext đã khai báo ở trên)
            await _hubContext.Clients.All.SendAsync("ReceivePaymentUpdate", contractId, "Paid");
        }
    }
}