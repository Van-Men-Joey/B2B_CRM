using AutoMapper;
using Customer_Relationship_Management.Models;
using Customer_Relationship_Management.ViewModels.AuditLog;
using Customer_Relationship_Management.ViewModels.Contract;
using Customer_Relationship_Management.ViewModels.Customer;
using Customer_Relationship_Management.ViewModels.Deal;
using Customer_Relationship_Management.ViewModels.SupportTicket;
using Customer_Relationship_Management.ViewModels.Task;
using Customer_Relationship_Management.ViewModels.User;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Customer_Relationship_Management.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {

            // Customer
            CreateMap<Customer, CustomerViewModel>().ReverseMap();

            // Deal
            CreateMap<Deal, DealViewModel>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.CompanyName))
                .ReverseMap();

            // Contract
            CreateMap<Contract, ContractViewModel>()
                .ReverseMap();

            // TaskItem
            CreateMap<Task, TaskViewModel>().ReverseMap();

            // EmployeeProfile
            CreateMap<User, EmployeeProfileViewModel>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName))
                .ReverseMap();

            // ManagerAccountControl
            CreateMap<User, ManagerAccountControlViewModel>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName));
        }
    }

}
