namespace Restaurant.Domain.Entities
{
    public class ProductAddOn
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int AddOnId { get; set; }
        public int HeadOfficeId { get; set; }
    }
}