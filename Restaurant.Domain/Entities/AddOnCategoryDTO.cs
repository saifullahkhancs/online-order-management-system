public class AddOnCategoryDTO
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? MinSelect { get; set; }
    public int? MaxSelect { get; set; }
    public int? SortOrder { get; set; }
}