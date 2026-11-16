using System;
using System.ComponentModel.DataAnnotations;

namespace BE1.Models
{
    [MetadataType(typeof(Category.CategoryMetadata))]
    public partial class Category
    {
        internal sealed class CategoryMetadata
        {
            [Key]
            public int CategoryID { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập tên danh mục!")]
            [Display(Name = "Tên danh mục")]
            public string CategoryName { get; set; }
        }
    }
}