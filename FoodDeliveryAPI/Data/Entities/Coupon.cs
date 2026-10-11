using System;
using System.Collections.Generic;

namespace FoodDeliveryAPI.Data.Entities;

public partial class Coupon
{
    public int CouponId { get; set; }

    public string Code { get; set; } = null!;

    public decimal DiscountPercent { get; set; }

    public decimal MinimumOrderAmount { get; set; }

    public int UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
