using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Dtos
{
    public class TaxDto
    {
        public int TaxId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Rate { get; set; }
        public bool IsPercentage { get; set; }
        public bool IsCompound { get; set; }
        public int? Priority { get; set; }
        public bool? IsActive { get; set; }
        public int? PaymentTypeId { get; set; }
        public string PaymentType { get; set; }
        public decimal Amount { get; set; }
    }

    //public class TaxDto
    //{
    //    public string TaxName { get; set; } = string.Empty;
    //    public decimal TaxRate { get; set; }
    //    public bool IsPercentage { get; set; }
    //    public string PaymentType { get; set; } = string.Empty;
    //    public decimal Amount { get; set; }
    //}

    public class CreateTaxRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Rate { get; set; }
        public bool IsPercentage { get; set; }
        public bool IsCompound { get; set; }
        public int? Priority { get; set; }
        public int? HeadOfficeId { get; set; }
        public int? PaymentTypeId { get; set; }
        public string PaymentType { get; set; }
        public bool? IsActive { get; set; }
        public List<int>? BranchIds { get; set; }
    }

    public class UpdateTaxRequest : CreateTaxRequest
    {
        public int TaxId { get; set; }
    }

    public class AssignBranchTaxRequest
    {
        public int BranchId { get; set; }
        public int TaxId { get; set; }
        public int? Priority { get; set; }
        public bool? IsCompound { get; set; }
        public bool? IsActive { get; set; }
    }
}
