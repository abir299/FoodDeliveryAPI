using System;
using System.Collections.Generic;

namespace FoodDeliveryAPI.Data.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }

    public string Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<DeliveryAssignment> DeliveryAssignments { get; set; } = new List<DeliveryAssignment>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
}
