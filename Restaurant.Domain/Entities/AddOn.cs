namespace Restaurant.Domain.Entities
{
    public class AddOn
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AddOnCategoryId { get; set; }
        public decimal? AddOnUnitPrice { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public int HeadOfficeId { get; set; }
    }
}