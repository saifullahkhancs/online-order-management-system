namespace Restaurant.Domain.Entities
{
    public class AddOnCategoryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public int? MinSelect { get; set; }
        public int? MaxSelect { get; set; }
        public bool? IsActive { get; set; } = true;
        public int? SortOrder { get; set; }
        public int HeadOfficeId { get; set; }
    }

    public class AddOnDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AddOnCategoryId { get; set; }
        public decimal? AddOnUnitPrice { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public int HeadOfficeId { get; set; }

        public AddOnCategoryDto? AddOnCategory { get; set; }
    }
}