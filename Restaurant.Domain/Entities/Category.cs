using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace Restaurant.Domain.Entities
{
    public partial class Category
    {
        public int CategoryId { get; set; }
        public int HeadOfficeId { get; set; }

        public string CategoryName { get; set; } = null!;

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }


    }
}