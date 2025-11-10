namespace Customer_Relationship_Management.ViewModels.Contract
{
    public class CreateContractViewModel
    {
        public int DealID { get; set; }
        public string ContractContent { get; set; } = null!;
        public string? FilePath { get; set; }
    }
}
