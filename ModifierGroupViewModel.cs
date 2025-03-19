namespace DAL.ViewModels;

public class ModifierGroupViewModel
{
    public int Groupid { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string SelectedItemIds { get; set; } 
}
