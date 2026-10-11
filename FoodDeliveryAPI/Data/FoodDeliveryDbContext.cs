using System;
using System.Collections.Generic;
using FoodDeliveryAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryAPI.Data;

public partial class FoodDeliveryDbContext : DbContext
{
    public FoodDeliveryDbContext()
    {
    }

    public FoodDeliveryDbContext(DbContextOptions<FoodDeliveryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<DeliveryAssignment> DeliveryAssignments { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<Restaurant> Restaurants { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=FoodDeliveryDbContext");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Carts__51BCD7B79B69699D");

            entity.HasIndex(e => e.CustomerId, "UQ__Carts__A4AE64D9743219F2").IsUnique();

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Customer).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Carts__CustomerI__5DCAEF64");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__488B0B0AC51238FE");

            entity.HasIndex(e => new { e.CartId, e.MenuItemId }, "UQ__CartItem__6928E8C40163E7D7").IsUnique();

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItems__CartI__619B8048");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItems__MenuI__628FA481");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B86EF92EB");

            entity.HasIndex(e => e.Name, "UQ__Categori__737584F62493BB46").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("PK__Coupons__384AF1BA0DE90836");

            entity.HasIndex(e => e.Code, "UQ__Coupons__A25C5AA706163DB2").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MinimumOrderAmount).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<DeliveryAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("PK__Delivery__32499E77C86C9B0C");

            entity.HasIndex(e => e.OrderId, "UQ__Delivery__C3905BCEE70E0746").IsUnique();

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.DeliveryPerson).WithMany(p => p.DeliveryAssignments)
                .HasForeignKey(d => d.DeliveryPersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DeliveryA__Deliv__00200768");

            entity.HasOne(d => d.Order).WithOne(p => p.DeliveryAssignment)
                .HasForeignKey<DeliveryAssignment>(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DeliveryA__Order__7F2BE32F");
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasKey(e => e.MenuItemId).HasName("PK__MenuItem__8943F72261C45EFB");

            entity.HasIndex(e => e.RestaurantId, "IX_MenuItems_RestaurantId");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.MenuItems)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MenuItems__Categ__5812160E");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.MenuItems)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MenuItems__Resta__571DF1D5");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF2FA5AAF1");

            entity.HasIndex(e => new { e.CustomerId, e.CreatedAt }, "IX_Orders_CustomerId_CreatedAt").IsDescending(false, true);

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_Orders_Status_CreatedAt");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.DeliveryAddress).HasMaxLength(300);
            entity.Property(e => e.DeliveryFee).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Coupon).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CouponId)
                .HasConstraintName("FK__Orders__CouponId__74AE54BC");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__Customer__72C60C4A");

            entity.HasOne(d => d.Restaurant).WithMany(p => p.Orders)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__Restaura__73BA3083");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED068145371FBF");

            entity.Property(e => e.ItemName).HasMaxLength(150);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__MenuI__797309D9");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Order__787EE5A0");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__OrderSta__4D7B4ABD817896C0");

            entity.ToTable("OrderStatusHistory");

            entity.HasIndex(e => new { e.OrderId, e.ChangedAt }, "IX_OrderStatusHistory_OrderId_ChangedAt").IsDescending(false, true);

            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NewStatus).HasMaxLength(30);
            entity.Property(e => e.OldStatus).HasMaxLength(30);

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderStat__Chang__04E4BC85");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderStat__Order__03F0984C");
        });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.HasKey(e => e.RestaurantId).HasName("PK__Restaura__87454C9532F9E3D3");

            entity.HasIndex(e => e.OwnerId, "IX_Restaurants_OwnerId");

            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(150);

            entity.HasOne(d => d.Owner).WithMany(p => p.Restaurants)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Restauran__Owner__5070F446");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C044C8668");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105341526613B").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Role)
                .HasMaxLength(30)
                .HasDefaultValue("Customer");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
