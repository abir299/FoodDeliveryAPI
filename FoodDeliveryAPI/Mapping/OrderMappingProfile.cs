using AutoMapper;
using FoodDeliveryAPI.Data.Entities;
using FoodDeliveryAPI.DTOs;

namespace FoodDeliveryAPI.Mapping;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<OrderItem, OrderItemResponseDto>();

        CreateMap<Order, OrderResponseDto>()
            .ForMember(
                dest => dest.RestaurantName,
                opt => opt.MapFrom(src => src.Restaurant.Name))
            .ForMember(
                dest => dest.Items,
                opt => opt.MapFrom(src => src.OrderItems));
    }
}
