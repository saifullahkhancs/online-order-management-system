namespace Restaurant.Domain.Entities
{
    public class ModifierCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        //public decimal Price { get; set; }
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; }
        public int HeadOfficeId { get; set; }
    }

    public class Modifier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal DefaultPrice { get; set; }
        public int HeadOfficeId { get; set; }
        public bool IsActive { get; set; }
    }
}