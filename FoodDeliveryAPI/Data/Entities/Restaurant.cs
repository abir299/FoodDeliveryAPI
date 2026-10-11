using System;
using System.Collections.Generic;

namespace FoodDeliveryAPI.Data.Entities;

public partial class Restaurant
{
    public int RestaurantId { get; set; }

    public int OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Address { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User Owner { get; set; } = null!;
}
