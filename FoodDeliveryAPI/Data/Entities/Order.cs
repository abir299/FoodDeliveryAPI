using System;
using System.Collections.Generic;

namespace FoodDeliveryAPI.Data.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public int RestaurantId { get; set; }

    public int? CouponId { get; set; }

    public string DeliveryAddress { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Coupon? Coupon { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual DeliveryAssignment? DeliveryAssignment { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual Restaurant Restaurant { get; set; } = null!;
}
