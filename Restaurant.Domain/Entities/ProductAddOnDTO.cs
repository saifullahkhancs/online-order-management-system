namespace Restaurant.Domain.Entities
{
    public class ProductAddOnDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int AddOnId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string AddOnName { get; set; } = string.Empty;
        public decimal? AddOnPrice { get; set; }
        public string? AddOnCategoryName { get; set; }
    }
}