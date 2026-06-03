namespace Restaurant.API.Models
{
    public class AddOnCategoryVM
    {
        public int Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public int? MinSelect { get; set; }
        public int? MaxSelect { get; set; }
        public bool? IsActive { get; set; } = true;
        public int? SortOrder { get; set; }
        public int HeadOfficeId { get; set; }
    }

    public class AddOnVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AddOnCategoryId { get; set; }
        public decimal? AddOnUnitPrice { get; set; }
        public bool? IsActive { get; set; } = true;
        public int HeadOfficeId { get; set; }
    }
}