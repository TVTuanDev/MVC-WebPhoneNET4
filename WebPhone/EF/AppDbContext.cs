using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace WebPhone.EF
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("Data Source=DESKTOP-PENP0E8\\SQLEXPRESS; Initial Catalog=WebPhoneNET; User Id=tvtuan; Password=123456") { }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<CategoryProduct> CategoryProducts { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Bill> Bills { get; set; }
        public virtual DbSet<BillInfo> BillInfos { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<UserRole> UserRoles { get; set; }
        public virtual DbSet<Warehouse> Warehouses { get; set; }
        public virtual DbSet<Inventory> Inventories { get; set; }
        public virtual DbSet<PaymentLog> PaymentLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<CategoryProduct>()
                .HasRequired(cp => cp.CateProductParent)
                .WithMany(cp => cp.CateProductChildren)
                .HasForeignKey(cp => cp.ParentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ProductName)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasRequired(p => p.CategoryProduct)
                .WithMany(cp => cp.Products)
                .HasForeignKey(p => p.CategoryId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Bill>()
                .HasRequired(b => b.Customer)
                .WithMany(c => c.CustomerBills)
                .HasForeignKey(b => b.CustomerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Bill>()
                .HasRequired(b => b.Employment)
                .WithMany(e => e.EmploymentBills)
                .HasForeignKey(b => b.EmploymentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BillInfo>()
                .HasRequired(bi => bi.Bill)
                .WithMany(b => b.BillInfos)
                .HasForeignKey(bi => bi.BillId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<BillInfo>()
                .HasRequired(bi => bi.Product)
                .WithMany(p => p.BillInfos)
                .HasForeignKey(bi => bi.ProductId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.RoleId, ur.UserId });

            modelBuilder.Entity<UserRole>()
                .HasRequired(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<UserRole>()
                .HasRequired(ur => ur.User)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<PaymentLog>()
                .HasRequired(pl => pl.Customer)
                .WithMany(c => c.PaymentLogs)
                .HasForeignKey(pl => pl.CustomerId);

            modelBuilder.Entity<PaymentLog>()
                .HasRequired(pl => pl.Bill)
                .WithMany(b => b.PaymentLogs)
                .HasForeignKey(pl => pl.BillId);

            modelBuilder.Entity<Inventory>()
                .HasRequired(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId);

            modelBuilder.Entity<Inventory>()
                .HasRequired(i => i.Warehouse)
                .WithMany(w => w.Inventories)
                .HasForeignKey(i => i.WarehouseId);
        }
    }
}