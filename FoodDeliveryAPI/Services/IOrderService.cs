using FoodDeliveryAPI.DTOs;

namespace FoodDeliveryAPI.Services;

public interface IOrderService
{
    Task<(OrderResponseDto? Data, string? Error)> PlaceOrderAsync(
        int customerId,
        PlaceOrderDto dto);

    Task<List<OrderResponseDto>> GetMyOrdersAsync(int customerId);

    Task<OrderResponseDto?> GetOrderAsync(int customerId, int orderId);
}