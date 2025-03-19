using BLL.IRepository;
using DAL.Database;
using DAL.Models;
using DAL.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BLL.Repository;

public class MenuItemsRepository : IMenuItemsRepository
{
    private readonly PizzaShopDbContext _db;
    private readonly IUserRepository _userRepository;
    public MenuItemsRepository(PizzaShopDbContext db, IUserRepository userRepository)
    {
        _db = db;
        _userRepository = userRepository;
    }
    public string AddCategories(Menucategory model)
    {
        _db.Menucategories.Add(model);
        _db.SaveChangesAsync();
        return "Categoty Added Successfully !";
    }
    public Menucategory GetCategory(int id)
    {
        return _db.Menucategories.FirstOrDefault(c => c.Categoryid == id && c.Isdeleted == false);
    }

    public Modifiergoup GetModifiers(int id)
    {
        return _db.Modifiergoups.FirstOrDefault(c => c.Groupid == id && c.Isdeleted == false);
    }

    public string EditCategory(int Id, string Name, string Description)
    {
        var user = GetCategory(Id);
        if (user == null)
        {
            return "User not found";
        }
        user.Name = Name;
        user.Description = Description;
        _db.SaveChanges();
        return "Edited Successfully";
    }

    public string DeleteCategories(int categoryId)
    {
        var category = _db.Menucategories.FirstOrDefault(c => c.Categoryid == categoryId);
        var item = _db.Menuitemsandmodifiers.FirstOrDefault(c => c.Categoryid == categoryId);
        if (category == null)
        {
            return "categoryId not found";
        }
        category.Isdeleted = true;
        if (item != null)
        {
            item.Isdeleted = true;
        }
        _db.SaveChanges();
        return "Category is Deleted !";
    }
    public async Task AddCategoriesAsync(Menucategory category)
    {
        await _db.Menucategories.AddAsync(category);
        await _db.SaveChangesAsync();
    }
    public async Task UpdateCategoryAsync(Menucategory category)
    {
        _db.Menucategories.Update(category);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateModifiersGroupAsync(Modifiergoup modifiergoup)
    {
        _db.Modifiergoups.Update(modifiergoup);
        await _db.SaveChangesAsync();
    }

    public async Task AddModifiersAsync(Modifiergoup Modifier)
    {
        await _db.Modifiergoups.AddAsync(Modifier);
        await _db.SaveChangesAsync();
    }

    public string DeleteModifierGrp(int Id)
    {
        var ModifierGrp = _db.Modifiergoups.FirstOrDefault(c => c.Groupid == Id);
        var item = _db.Menuitemsandmodifiers.FirstOrDefault(c => c.Categoryid == Id && c.Ismodifier == true);
        if (ModifierGrp == null)
        {
            return "Id not found";
        }
        ModifierGrp.Isdeleted = true;
        if (item != null)
        {
            item.Isdeleted = true;
        }
        _db.SaveChanges();
        return "Modifier is Deleted !";
    }

    public async Task AddModifierItemAsync(Itemmodifiergroupmapping modifierItem)
    {
        await _db.Itemmodifiergroupmappings.AddAsync(modifierItem);
        await _db.SaveChangesAsync();
    }

    public async Task<List<CategoryViewModel>> GetCategoriesAsync()
    {
        return await _db.Menucategories
            .Where(c => (bool)!c.Isdeleted)
            .Select(c => new CategoryViewModel
            {
                Categoryid = c.Categoryid,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync();
    }

    public async Task<List<ItemViewModel>> GetItemsByCategoryAsync(int categoryid)
    {
        return await _db.Menuitemsandmodifiers
            .Where(c => !c.Isdeleted && !c.Ismodifier && c.Categoryid == categoryid)
            .Select(i => new ItemViewModel
            {
                Itemid = i.Itemid,
                Name = i.Name,
                Rate = i.Rate ?? 0,
                Quantity = i.Quantity,
                Itemtype = i.Itemtype,
                Isavailable = i.Isavailable,
                Itemimage = i.Itemimage ?? "~/images/dinning-menu.png",
                Unit = i.Unit
            })
            .ToListAsync();
    }

    public async Task<List<ModifierGroupViewModel>> GetModifierGroupsAsync()
    {
        return await _db.Modifiergoups
            .Where(c => (bool)!c.Isdeleted)
            .Select(c => new ModifierGroupViewModel
            {
                Groupid = c.Groupid,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync();
    }

    public async Task<(List<ItemViewModel> Items, int TotalItems)> GetItemsByCategoryAsync(int categoryId, int page, int pageSize, string searchTerm)
    {
        var itemsQuery = _db.Menuitemsandmodifiers
                            .Where(c => c.Categoryid == categoryId && !c.Isdeleted && !c.Ismodifier);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            itemsQuery = itemsQuery.Where(c => c.Name.ToLower().Contains(searchTerm));
        }

        int totalItems = await itemsQuery.CountAsync();

        var paginatedItems = await itemsQuery
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .Select(item => new ItemViewModel
                            {
                                Itemid = item.Itemid,
                                Name = item.Name,
                                Rate = item.Rate,
                                Itemtype = item.Itemtype,
                                Unit = item.Unit,
                                Quantity = item.Quantity,
                                Isavailable = item.Isavailable,
                                Itemimage = item.Itemimage,
                                Tax = item.Tax,
                                ItemShortCode = item.Itemshortcode,
                                Description = item.Description,
                                CategoryId = item.Categoryid
                            })
                            .ToListAsync();

        return (paginatedItems, totalItems);
    }

    public async Task<List<ItemViewModel>> GetAllModifierItemsAsync()
    {
        return await _db.Menuitemsandmodifiers
            .Where(item => item.Ismodifier == true && item.Isdeleted == false)
            .Select(item => new ItemViewModel
            {
                Itemid = item.Itemid,
                Name = item.Name,
                Unit = item.Unit,
                Rate = item.Rate ?? 0,
                Quantity = item.Quantity
            })
            .ToListAsync();
    }

    public async Task<MenuItemViewModel> GetModifierItemsByModifierGroupAsync(int categoryid, string searchTerm, int page, int pageSize)
    {
        var query = _db.Itemmodifiergroupmappings
            .Where(c => c.Modifiergroupid == categoryid)
            .Join(_db.Menuitemsandmodifiers,
                mapping => mapping.Itemid,
                item => item.Itemid,
                (mapping, item) => new ItemViewModel
                {
                    Itemid = item.Itemid,
                    Name = item.Name,
                    Unit = item.Unit,
                    Rate = item.Rate ?? 0,
                    Quantity = item.Quantity,
                    IsDeleted = item.Isdeleted
                })
            .Where(item => item.Quantity > 0 && !item.IsDeleted);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(item => item.Name.Contains(searchTerm));
        }

        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var paginatedItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new MenuItemViewModel
        {
            Items = paginatedItems,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            SelectedCategoryId = categoryid
        };
    }

    public async Task<bool> AddModifierItemAsync(Menuitemsandmodifier model, List<int> categoryIds)
    {
        if (model == null || categoryIds == null || categoryIds.Count == 0)
            return false;

        var menuItem = new Menuitemsandmodifier
        {
            Name = model.Name,
            Rate = model.Rate,
            Quantity = model.Quantity,
            Unit = model.Unit,
            Description = model.Description,
            Ismodifier = true
        };

        await _db.Menuitemsandmodifiers.AddAsync(menuItem);
        await _db.SaveChangesAsync();

        var itemMappings = categoryIds.Select(categoryId => new Itemmodifiergroupmapping
        {
            Modifiergroupid = categoryId,
            Itemid = menuItem.Itemid
        }).ToList();

        await _db.Itemmodifiergroupmappings.AddRangeAsync(itemMappings);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AddModifiers(Modifiergoup modifierGroup)
    {
        if (modifierGroup == null) return false;

        await _db.Modifiergoups.AddAsync(modifierGroup);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddModifierItem(Itemmodifiergroupmapping modifierItem)
    {
        if (modifierItem == null) return false;

        await _db.Itemmodifiergroupmappings.AddAsync(modifierItem);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Menuitemsandmodifier> GetMenuItemByIdAsync(int itemId)
    {
        return await _db.Menuitemsandmodifiers
            .FirstOrDefaultAsync(i => i.Itemid == itemId);
    }

    // Fetch Modifier Group by ID
    public async Task<ModifierGroupViewModel> GetModifierGroupAsync(int id)
    {
        var modifierGroup = await _db.Modifiergoups.FirstOrDefaultAsync(m => m.Groupid == id);

        if (modifierGroup == null)
            return null;

        return new ModifierGroupViewModel
        {
            Groupid = modifierGroup.Groupid,
            Name = modifierGroup.Name,
            Description = modifierGroup.Description
        };
    }

    // Update Modifier Group
    public async Task<bool> UpdateModifierGroupAsync(ModifierGroupViewModel model)
    {
        var modifier = await _db.Modifiergoups.FirstOrDefaultAsync(m => m.Groupid == model.Groupid);
        if (modifier == null)
            return false;

        modifier.Name = model.Name;
        modifier.Description = model.Description;

        _db.Modifiergoups.Update(modifier);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Menuitemsandmodifier> GetModifierItemByIdAsync(int itemId)
    {
        var item = await _db.Menuitemsandmodifiers.FirstOrDefaultAsync(i => i.Itemid == itemId);
        if (item == null)
            return null;

        return new Menuitemsandmodifier
        {
            Itemid = item.Itemid,
            Name = item.Name,
            Rate = item.Rate ?? 0,
            Quantity = item.Quantity,
            Unit = item.Unit,
            Description = item.Description,
            Categoryid = await _db.Itemmodifiergroupmappings
                                 .Where(m => m.Itemid == item.Itemid)
                                 .Select(m => m.Modifiergroupid)
                                 .FirstOrDefaultAsync()
        };
    }

    public async Task<bool> EditModifierItemAsync(Menuitemsandmodifier item)
    {
        var existingItem = await _db.Menuitemsandmodifiers.FirstOrDefaultAsync(i => i.Itemid == item.Itemid);

        if (existingItem == null)
        {
            return false; // Item not found
        }

        // Update item properties
        existingItem.Name = item.Name;
        existingItem.Rate = item.Rate;
        existingItem.Quantity = item.Quantity;
        existingItem.Unit = item.Unit;
        existingItem.Description = item.Description;

        // Update or create modifier group mapping
        var existingMapping = await _db.Itemmodifiergroupmappings.FirstOrDefaultAsync(m => m.Itemid == item.Itemid);
        if (existingMapping != null)
        {
            existingMapping.Modifiergroupid = item.Categoryid;
        }
        else
        {
            await _db.Itemmodifiergroupmappings.AddAsync(new Itemmodifiergroupmapping
            {
                Itemid = item.Itemid,
                Modifiergroupid = item.Categoryid
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateMenuItemAsync(int id, MenuItemViewModel model)
    {
        var item = _db.Menuitemsandmodifiers.FirstOrDefault(m => m.Itemid == id);
        if (item == null)
        {
            return false;
        }

        var itemData = model.Items.FirstOrDefault();
        if (itemData == null)
        {
            return false;
        }

        item.Name = itemData.Name;
        item.Categoryid = itemData.CategoryId ?? item.Categoryid;
        item.Itemtype = itemData.Itemtype;
        item.Rate = itemData.Rate ?? item.Rate;
        item.Quantity = itemData.Quantity;
        item.Unit = itemData.Unit;
        item.Isavailable = itemData.Isavailable;
        item.Tax = itemData.Tax ?? item.Tax;
        item.Itemshortcode = itemData.ItemShortCode;
        item.Description = itemData.Description;

        if (model.ItemPhoto != null)
        {
            item.Itemimage = await UploadImageAsync(model.ItemPhoto);
        }

        _db.Menuitemsandmodifiers.Update(item);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return string.Empty;
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/userimages/userprofile");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return "userimages/userprofile/" + uniqueFileName;
    }

    public async Task<bool> EditCategoryAsync(CategoryViewModel model)
    {
        var category = await _db.Menucategories
            .FirstOrDefaultAsync(u => u.Categoryid == model.Categoryid && u.Isdeleted == false);

        if (category == null)
        {
            return false;
        }

        category.Name = model.Name;
        category.Description = model.Description;

        _db.Menucategories.Update(category);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AddNewItemAsync(ItemViewModel model)
    {
        try
        {
            var menuItem = new Menuitemsandmodifier
            {
                Categoryid = model.CategoryId,
                Name = model.Name,
                Itemtype = model.Itemtype,
                Rate = model.Rate,
                Quantity = model.Quantity,
                Unit = model.Unit,
                Isavailable = model.Isavailable,
                Tax = model.Tax,
                Itemshortcode = model.ItemShortCode,
                Description = model.Description,
                Itemimage = model.Itemimage == null
                    ? "~/images/dining-menu.png"
                    : _userRepository.ImageUpload(model.ItemPhoto)
            };

            _db.Menuitemsandmodifiers.Add(menuItem);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
