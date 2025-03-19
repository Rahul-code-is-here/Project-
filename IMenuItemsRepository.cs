using DAL.Models;
using DAL.ViewModels;
using Microsoft.AspNetCore.Http;

namespace BLL.IRepository;

public interface IMenuItemsRepository
{
    string AddCategories(Menucategory model);
    Menucategory GetCategory(int id);
    string EditCategory(int Id, string Name, string Description);
    string DeleteCategories(int categoryId);
    Task AddCategoriesAsync(Menucategory category);
    Task UpdateCategoryAsync(Menucategory category);
    Task AddModifiersAsync(Modifiergoup Modifier);
    Modifiergoup GetModifiers(int id);
    Task UpdateModifiersGroupAsync(Modifiergoup modifiergoup);
    string DeleteModifierGrp(int Id);
    Task AddModifierItemAsync(Itemmodifiergroupmapping modifierItem);
    Task<List<CategoryViewModel>> GetCategoriesAsync();
    Task<List<ItemViewModel>> GetItemsByCategoryAsync(int categoryid);
    Task<List<ModifierGroupViewModel>> GetModifierGroupsAsync();
    Task<(List<ItemViewModel> Items, int TotalItems)> GetItemsByCategoryAsync(int categoryId, int page, int pageSize, string searchTerm);
    Task<List<ItemViewModel>> GetAllModifierItemsAsync();
    Task<MenuItemViewModel> GetModifierItemsByModifierGroupAsync(int categoryid, string searchTerm, int page, int pageSize);
    Task<bool> AddModifierItemAsync(Menuitemsandmodifier model, List<int> categoryIds);
    Task<bool> AddModifiers(Modifiergoup modifierGroup);
    Task<bool> AddModifierItem(Itemmodifiergroupmapping modifierItem);
    Task<Menuitemsandmodifier> GetMenuItemByIdAsync(int itemId);
    Task<ModifierGroupViewModel> GetModifierGroupAsync(int id);
    Task<bool> UpdateModifierGroupAsync(ModifierGroupViewModel model);
    Task<Menuitemsandmodifier> GetModifierItemByIdAsync(int itemId);
    Task<bool> EditModifierItemAsync(Menuitemsandmodifier item);
    Task<bool> UpdateMenuItemAsync(int id, MenuItemViewModel model);
    Task<string> UploadImageAsync(IFormFile file);
    Task<bool> EditCategoryAsync(CategoryViewModel model);
    Task<bool> AddNewItemAsync(ItemViewModel model);
}
