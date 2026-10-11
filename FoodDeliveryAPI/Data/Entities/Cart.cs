using System;
using System.Collections.Generic;

namespace FoodDeliveryAPI.Data.Entities;

public partial class Cart
{
    public int CartId { get; set; }

    public int CustomerId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual User Customer { get; set; } = null!;
}
