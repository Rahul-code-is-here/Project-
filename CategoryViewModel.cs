using System.ComponentModel.DataAnnotations;

namespace DAL.ViewModels;

public class CategoryViewModel
{
    public int Categoryid { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "Description is required.")]
    public string? Description { get; set; }
}
