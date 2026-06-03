namespace Restaurant.API.Models
{
    public class ProductAddOnVM
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int AddOnId { get; set; }
        public int HeadOfficeId { get; set; }
    }
}