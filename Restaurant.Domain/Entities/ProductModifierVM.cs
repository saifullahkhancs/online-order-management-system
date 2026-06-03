namespace Restaurant.API.Models
{
    public class ProductModifierVM
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ModifierId { get; set; }
        public int HeadOfficeId { get; set; }
    }
}