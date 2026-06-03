using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long? OrderId { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal? AmountPaid { get; set; }

    public string? TransactionRef { get; set; }

    public string? PaymentGateway { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? GatewayResponse { get; set; }

    public int? CardMachineId { get; set; }

    public int? HandledByUserId { get; set; }

    public virtual CardMachine? CardMachine { get; set; }

    public virtual User? HandledByUser { get; set; }

    public virtual Order? Order { get; set; }
}
