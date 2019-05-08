using System;
namespace CognitiveBot.Model
{
    public class ShoppingCart
    {
        public int ProductID { get; set; }
        public int CustomerID { get; set; }
        public string ProductName { get; set; }
        public string Photo { get; set; }
        public decimal ListPrice { get; set; }
    }
}
