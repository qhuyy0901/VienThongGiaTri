using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetMvcApp.Models;

public class ProductSpecification
{
    public int Id { get; set; }
    
    public int ProductId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Value { get; set; } = string.Empty;
    
    public int DisplayOrder { get; set; }

    // Navigation property
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}
