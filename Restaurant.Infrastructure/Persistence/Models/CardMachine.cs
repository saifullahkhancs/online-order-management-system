using System;
using System.Collections.Generic;

namespace Restaurant.Infrastructure.Persistence.Models;

public partial class CardMachine
{
    public int CardMachineId { get; set; }

    public string? MachineName { get; set; }

    public string? MachineLocation { get; set; }

    public string? MachineStatus { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
