# FoodDeliveryAPI

A role-based Food Delivery REST API built with ASP.NET Core Web API, Entity Framework Core, and SQL Server.

## Features

- JWT Authentication and Role-Based Authorization
- User Registration and Login
- Restaurant and Menu Management
- Categories and Menu Items
- Shopping Cart Management
- Order Placement and Processing
- Coupon Validation
- Order Status Tracking and History
- Admin Dashboard
- Delivery Assignment and Tracking

## Technologies Used

- ASP.NET Core Web API
- C#
- Entity Framework Core
- Microsoft SQL Server
- JWT Authentication
- Postman

## User Roles

- **Admin** — Dashboard, order management, and delivery assignment
- **RestaurantOwner** — Restaurant and menu management
- **Customer** — Browse menus, manage cart, and place orders
- **DeliveryPerson** — View assignments and update delivery status

## Getting Started

1. Clone this repository.
2. Open `FoodDeliveryAPI.slnx` in Visual Studio.
3. Update the database connection string in `appsettings.json` for your SQL Server instance.
4. Ensure the `FoodDeliveryDb` database and required tables exist.
5. Run the project.
6. Test the API endpoints using Postman.

## API Base URL

`https://localhost:7002`

## Author

**Abir Ahmed**

GitHub: [abir299](https://github.com/abir299)