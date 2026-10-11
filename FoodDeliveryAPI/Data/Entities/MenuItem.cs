using System;
using System.Collections.Generic;

namespace FoodDeliveryAPI.Data.Entities;

public partial class MenuItem
{
    public int MenuItemId { get; set; }

    public int RestaurantId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Restaurant Restaurant { get; set; } = null!;
}
