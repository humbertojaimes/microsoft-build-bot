using System;
namespace CognitiveBot.Model
{
    public class Product_Model
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public decimal ListPrice { get; set; }
        //public string Photo { get; set; }
        public byte[] PhotoBytes { get; set; }
        public string Category { get; set; }
        public string Model { get; set; }

        //public byte[] PhotoBytes => Convert.FromBase64String(Photo);
        public string Photo => Convert.ToBase64String(PhotoBytes);

    }
}
