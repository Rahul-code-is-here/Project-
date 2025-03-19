using Microsoft.AspNetCore.Http;

namespace DAL.ViewModels;

public class ItemViewModel
{
    public int Itemid { get; set; }
    public string Name { get; set; }
    public decimal? Rate { get; set; }
    public bool Itemtype { get; set; }
    public string Unit { get; set; }
    public decimal Quantity { get; set; }
    public bool Isavailable { get; set; }
    public string? Itemimage { get; set; } = "";
    public IFormFile ItemPhoto { get; set; }
    public decimal? Tax { get; set; }
    public string ItemShortCode { get; set; }
    public string Description { get; set; }
    public int? CategoryId  { get; set; }
    public bool IsDeleted { get; set; }

    public static implicit operator List<object>(ItemViewModel v)
    {
        throw new NotImplementedException();
    }

}