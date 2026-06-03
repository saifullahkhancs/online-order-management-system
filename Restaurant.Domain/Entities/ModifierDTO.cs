namespace Restaurant.Domain.Entities
{
    public class ModifierCategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        //public decimal Price { get; set; }
        public bool? IsRequired { get; set; } = false;
        public bool? IsActive { get; set; } = true;
        public int? HeadOfficeId { get; set; }
    }

    public class ModifierDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal DefaultPrice { get; set; }
        public string? CategoryName { get; set; }
        public int? HeadOfficeId { get; set; }
    }
}