namespace WebPhone.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitDatabase : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BillInfos",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        ProductId = c.Guid(nullable: false),
                        BillId = c.Guid(nullable: false),
                        ProductName = c.String(nullable: false, maxLength: 500),
                        Price = c.Int(nullable: false),
                        Discount = c.Int(),
                        Quantity = c.Int(nullable: false),
                        CreateAt = c.DateTime(nullable: false),
                        UpdateAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Bills", t => t.BillId, cascadeDelete: true)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.ProductId)
                .Index(t => t.BillId);
            
            CreateTable(
                "dbo.Bills",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        CustomerId = c.Guid(nullable: false),
                        EmploymentId = c.Guid(nullable: false),
                        CustomerName = c.String(nullable: false, maxLength: 200),
                        EmploymentName = c.String(nullable: false, maxLength: 200),
                        Price = c.Int(nullable: false),
                        DiscountStyle = c.Int(nullable: false),
                        DiscountStyleValue = c.Int(nullable: false),
                        Discount = c.Int(),
                        TotalPrice = c.Int(nullable: false),
                        PaymentPrice = c.Int(nullable: false),
                        CreateAt = c.DateTime(nullable: false),
                        UpdateAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.CustomerId)
                .ForeignKey("dbo.Users", t => t.EmploymentId)
                .Index(t => t.CustomerId)
                .Index(t => t.EmploymentId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 200),
                        Email = c.String(nullable: false, maxLength: 100),
                        PhoneNumber = c.String(maxLength: 15),
                        Address = c.String(maxLength: 500),
                        PasswordHash = c.String(maxLength: 200),
                        EmailConfirmed = c.Boolean(nullable: false),
                        CreateAt = c.DateTime(nullable: false),
                        UpdateAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName)
                .Index(t => t.Email, unique: true);
            
            CreateTable(
                "dbo.PaymentLogs",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        BillId = c.Guid(nullable: false),
                        CustomerId = c.Guid(nullable: false),
                        Price = c.Int(nullable: false),
                        CreateAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Bills", t => t.BillId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.CustomerId, cascadeDelete: true)
                .Index(t => t.BillId)
                .Index(t => t.CustomerId);
            
            CreateTable(
                "dbo.UserRoles",
                c => new
                    {
                        RoleId = c.Guid(nullable: false),
                        UserId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.RoleId, t.UserId })
                .ForeignKey("dbo.Roles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.RoleId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        RoleName = c.String(nullable: false, maxLength: 200),
                        CreateAt = c.DateTime(nullable: false),
                        UpdateAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.RoleName, unique: true);
            
            CreateTable(
                "dbo.Products",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        ProductName = c.String(nullable: false, maxLength: 500),
                        Description = c.String(),
                        Price = c.Int(nullable: false),
                        Discount = c.Int(),
                        CreateAt = c.DateTime(nullable: false),
                        UpdateAt = c.DateTime(),
                        CategoryId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CategoryProducts", t => t.CategoryId, cascadeDelete: true)
                .Index(t => t.ProductName, unique: true)
                .Index(t => t.CategoryId);
            
            CreateTable(
                "dbo.CategoryProducts",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        CategoryName = c.String(nullable: false, maxLength: 100),
                        CreateAt = c.DateTime(nullable: false),
                        UpdateAt = c.DateTime(),
                        ParentId = c.Guid(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CategoryProducts", t => t.ParentId)
                .Index(t => t.ParentId);
            
            CreateTable(
                "dbo.Inventories",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        ProductId = c.Guid(nullable: false),
                        WarehouseId = c.Guid(nullable: false),
                        Quantity = c.Int(nullable: false),
                        ImportPrice = c.Int(nullable: false),
                        CreateAt = c.DateTime(nullable: false),
                        UpdateAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .ForeignKey("dbo.Warehouses", t => t.WarehouseId, cascadeDelete: true)
                .Index(t => t.ProductId)
                .Index(t => t.WarehouseId);
            
            CreateTable(
                "dbo.Warehouses",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        WarehouseName = c.String(nullable: false, maxLength: 200),
                        Address = c.String(nullable: false, maxLength: 200),
                        Capacity = c.Int(nullable: false),
                        CreateAt = c.DateTime(nullable: false),
                        UpdateAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.BillInfos", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Inventories", "WarehouseId", "dbo.Warehouses");
            DropForeignKey("dbo.Inventories", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Products", "CategoryId", "dbo.CategoryProducts");
            DropForeignKey("dbo.CategoryProducts", "ParentId", "dbo.CategoryProducts");
            DropForeignKey("dbo.BillInfos", "BillId", "dbo.Bills");
            DropForeignKey("dbo.Bills", "EmploymentId", "dbo.Users");
            DropForeignKey("dbo.Bills", "CustomerId", "dbo.Users");
            DropForeignKey("dbo.UserRoles", "UserId", "dbo.Users");
            DropForeignKey("dbo.UserRoles", "RoleId", "dbo.Roles");
            DropForeignKey("dbo.PaymentLogs", "CustomerId", "dbo.Users");
            DropForeignKey("dbo.PaymentLogs", "BillId", "dbo.Bills");
            DropIndex("dbo.Inventories", new[] { "WarehouseId" });
            DropIndex("dbo.Inventories", new[] { "ProductId" });
            DropIndex("dbo.CategoryProducts", new[] { "ParentId" });
            DropIndex("dbo.Products", new[] { "CategoryId" });
            DropIndex("dbo.Products", new[] { "ProductName" });
            DropIndex("dbo.Roles", new[] { "RoleName" });
            DropIndex("dbo.UserRoles", new[] { "UserId" });
            DropIndex("dbo.UserRoles", new[] { "RoleId" });
            DropIndex("dbo.PaymentLogs", new[] { "CustomerId" });
            DropIndex("dbo.PaymentLogs", new[] { "BillId" });
            DropIndex("dbo.Users", new[] { "Email" });
            DropIndex("dbo.Users", new[] { "UserName" });
            DropIndex("dbo.Bills", new[] { "EmploymentId" });
            DropIndex("dbo.Bills", new[] { "CustomerId" });
            DropIndex("dbo.BillInfos", new[] { "BillId" });
            DropIndex("dbo.BillInfos", new[] { "ProductId" });
            DropTable("dbo.Warehouses");
            DropTable("dbo.Inventories");
            DropTable("dbo.CategoryProducts");
            DropTable("dbo.Products");
            DropTable("dbo.Roles");
            DropTable("dbo.UserRoles");
            DropTable("dbo.PaymentLogs");
            DropTable("dbo.Users");
            DropTable("dbo.Bills");
            DropTable("dbo.BillInfos");
        }
    }
}
