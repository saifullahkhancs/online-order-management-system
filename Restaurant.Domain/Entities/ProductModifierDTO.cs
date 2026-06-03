namespace Restaurant.Domain.Entities
{
    public class ProductModifierDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ModifierId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ModifierName { get; set; } = string.Empty;
        public decimal ModifierPrice { get; set; }
        public string? ModifierCategoryName { get; set; }
    }
}