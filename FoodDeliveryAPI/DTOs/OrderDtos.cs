using System;
using System.Collections.Generic;

namespace FoodDeliveryAPI.DTOs;

public class PlaceOrderDto
{
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
}

public class OrderItemResponseDto
{
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<OrderItemResponseDto> Items { get; set; } = new();
}
