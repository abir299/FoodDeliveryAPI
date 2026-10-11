using System;
using System.Collections.Generic;

namespace FoodDeliveryAPI.Data.Entities;

public partial class DeliveryAssignment
{
    public int AssignmentId { get; set; }

    public int OrderId { get; set; }

    public int DeliveryPersonId { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public virtual User DeliveryPerson { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
