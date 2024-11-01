using System;
using System.Collections.Generic;

namespace WebPhone.Models.Warehouses
{
    public class InventoryDTO
    {
        public Guid WarehouseId { get; set; }
        public List<ProductImport> ProductImports { get; set; }
    }

    public class ProductImport
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public int ImportPrice { get; set; }
    }
}