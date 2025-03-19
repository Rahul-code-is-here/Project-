using BLL.IRepository;
using DAL.Database;
using DAL.Models;
using DAL.ViewModels;
using Microsoft.EntityFrameworkCore;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BLL.Repository;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Authorization;
using System;

namespace PizzaShop.Controllers;

[Authorize]
[Route("[controller]")]
public class UserViewController : Controller
{
    private readonly PizzaShopDbContext _db;
    private readonly IUserRepository _userRepository;
    private readonly IGetDataRepository _getDataRepository;
    private readonly IEmailSender _emailSender;
    private readonly IPasswordHasherRepository _passwordHasherRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMenuItemsRepository _menuRepository;
    protected string UserEmail { get; set; } = "";

    public UserViewController(PizzaShopDbContext db, IUserRepository userRepository, IGetDataRepository getDataRepository, IPasswordHasherRepository passwordHasherRepository, IEmailSender emailSender, IPermissionRepository permissionRepository, IMenuItemsRepository menuRepository)
    {
        _db = db;
        _userRepository = userRepository;
        _getDataRepository = getDataRepository;
        _passwordHasherRepository = passwordHasherRepository;
        _emailSender = emailSender;
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
    }

    [HttpGet("MyProfile")]
    public IActionResult MyProfile()
    {

        var token = Request.Cookies["jwtToken"];
        UserEmail = _userRepository.GetUserEmailFromJwtToken(token);
        if (UserEmail == "")
        {
            return RedirectToAction("LoginPage", "Login");
        }



        var user = _userRepository.GetUser(UserEmail);
        if (user == null)
        {
            return NotFound("User Not Found");
        }
        return View(_userRepository.FetchUserDetail(user));
    }

    [HttpPost("MyProfile")]
    [ValidateAntiForgeryToken]
    public IActionResult MyProfile(UsertableViewModel model)
    {
        if (ModelState.IsValid)
        {
            var token = Request.Cookies["jwtToken"];
            UserEmail = _userRepository.GetUserEmailFromJwtToken(token);
            if (UserEmail == "")
            {
                return RedirectToAction("LoginPage", "Login");
            }
            var user = _userRepository.GetUser(UserEmail);
            if (user == null)
            {
                return NotFound("User Not Found");
            }
            if (model.profile == null)
            {
                model.profile = _userRepository.RetrieveImageAsIFormFile(user.Images);
            }
            _userRepository.UpdateUser(model, user);
            TempData["success"] = "Profile updated Successfully";
            return RedirectToAction("MyProfile");
        }
        if (model.State == null)
        {
            TempData["error"] = "State is Reqiured";
            return RedirectToAction("MyProfile");
        }
        if (model.City == null)
        {
            TempData["error"] = "City is Reqiured";
            return RedirectToAction("MyProfile");
        }
        if (model.Country == null)
        {
            ModelState.AddModelError("EmptyCity", "City is required");
            return RedirectToAction("MyProfile");
        }
        return View(model);
    }

    [HttpGet("ChangePassword")]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost("ChangePassword")]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var token = Request.Cookies["jwtToken"];
            UserEmail = _userRepository.GetUserEmailFromJwtToken(token);
            var loginuser = _userRepository.GetEntityByField<Login>(u => u.Email == UserEmail);
            var user = _userRepository.GetEntityByField<Usertable>(u => u.Email == UserEmail);
            if (user == null || loginuser == null)
            {
                return RedirectToAction("Login", "Home");
            }
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, loginuser.Password))
            {
                TempData["Error"] = "Current password is incorrect.";
                ModelState.AddModelError("", "Current password is incorrect.");
                return View(model);
            }
            var newPasswordHash = _passwordHasherRepository.HashPassword(model.NewPassword);
            _userRepository.ChangePassword(loginuser, user, newPasswordHash);
            TempData["success"] = "Password changed successfully!";
            return RedirectToAction("MyProfile");
        }
        return View(model);
    }

    [HttpGet("Userlist")]
    public async Task<IActionResult> Userlist(
    string searchTerm = "",
    int page = 1,
    int itemsPerPage = 5,
    string sortColumn = "Name",
    string sortOrder = "asc")
    {
        var email = Request.Cookies["UserEmail"];
        var (users, totalUsers, totalPages) = await _userRepository.GetPaginatedUsersAsync(email, searchTerm, page, itemsPerPage, sortColumn, sortOrder);

        ViewData["SearchTerm"] = searchTerm;
        ViewBag.page = page;
        ViewData["TotalPages"] = totalPages;
        ViewBag.itemsPerPage = itemsPerPage;
        ViewBag.totalUsers = totalUsers;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortOrder = sortOrder;

        return View(users);
    }

    //Add User get Method for Userlist
    [CustomAuthorize("Admin")]
    [HttpGet("AddUser")]
    public IActionResult AddUser()
    {
        return View();
    }

    [HttpPost("AddUser")]
    public IActionResult AddUser(AddUserViewModel model)
    {
        if (ModelState.IsValid)
        {   
            _userRepository.SetCountryStateCity(model);
            var role = _userRepository.GetRole(model);
            if (role == null)
            {
                ModelState.AddModelError(string.Empty, "The selected role is invalid.");
                return View(model);
            }

            if (_db.Usertables.Any(u => u.Email == model.Email))
            {
                TempData["error"] = "Email already exists";
                return View(model);
            }
            if (_db.Usertables.Any(u => u.Username == model.Username))
            {
                TempData["error"] = "Username already exists";
                return View(model);
            }
            if(_db.Usertables.Any(u => u.Phone == model.Phone))
            {
                TempData["error"] = "Phonr number is already Exist";
                return View(model);
            }
            _userRepository.AddUser(role, model);
            TempData["success"] = "User Added Successfully.";

            return RedirectToAction("UserList");
        }
        return View(model);
    }

    [HttpGet("EditUser/{Email}")]
    public IActionResult EditUser(string Email)
    {
        var user = _userRepository.GetEntityByField<Usertable>(u => u.Email == Email);
        if (user == null)
        {
            return RedirectToAction("Userlist", "UserView");
        }
        var role = _userRepository.GetRole(user);

        return View(_userRepository.GetDataforEditPage(user, role));
    }

    [HttpPost("EditUser/{Email}")]
    public IActionResult EditUser(EditUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = _userRepository.GetEntityByField<Usertable>(u => u.Email == model.Email);
            if (user == null)
            {
                return RedirectToAction("Userlist", "UserView");
            }
            if (model.State == null)
            {
                TempData["error"] = "State is required";
            }
            if (model.City == null)
            {
                TempData["error"] = "City is required";
            }
            if (model.Country == null)
            {
                TempData["error"] = "Country is required";
            }
            _userRepository.EditUser(user, model);
            TempData["success"] = "User update Successfully";
            return Json(new { success = true, redirectUrl = "/UserView/Userlist" });
        }
        return View(model);
    }

    [HttpGet("DeleteUser")]
    public IActionResult DeleteUser(string Email)
    {
        var user = _userRepository.GetEntityByField<Usertable>(u => u.Email == Email);
        user.Isdeleted = true;
        _db.SaveChanges();
        TempData["success"] = "User Deleted Successfully";
        return RedirectToAction("Userlist");
    }

    [HttpGet("Role")]
    public IActionResult Roles()
    {
        var roles = _userRepository.GetRoles();
        return View(roles);
    }

    [HttpGet("Permission")]
    public async Task<IActionResult> Permission(int roleId)
    {
        var permissions = await _permissionRepository.GetPermissionsByRoleId(roleId);
        var Rolename = _userRepository.GetRoleName(roleId);
        var permissionViewModels = permissions.Select(p => new PermissionViewModel
        {
            PermissionName = p.PermissionName,
            CanView = p.CanView,
            CanAddEdit = p.CanAddEdit,
            CanDelete = p.CanDelete,
            IsChecked = p.IsChecked
        }).ToList();

        ViewData["Rolename"] = Rolename;
        return View(permissionViewModels);
    }

    [HttpPost("SavePermissions")]
    public async Task<IActionResult> SavePermissions(int roleId, List<PermissionViewModel> permissions)
    {
        foreach (var permission in permissions)
        {
            // Debugging: log the MenuId value
            Console.WriteLine($"MenuId: {permission.MenuId}");  // Check if it's 0

            // if (permission.MenuId == 0)
            // {
            //     TempData["error"] = "Invalid MenuId.";
            //     return RedirectToAction("ErrorPage");  // Or handle as needed
            // }

            var menuExists = await _db.Navigationmenus.AnyAsync(m => m.Menuid == permission.MenuId);
            // if (!menuExists)
            // {
            //     TempData["error"] = $"Menu with ID {permission.MenuId} does not exist.";
            //     return RedirectToAction("ErrorPage");  // Or handle as needed
            // }

            var permissionEntity = await _db.Permissions
                .FirstOrDefaultAsync(p => p.Roleid == roleId && p.Menuid == permission.MenuId);

            if (permissionEntity != null)
            {
                permissionEntity.Canview = permission.CanView;
                permissionEntity.Canaddedit = permission.CanAddEdit;
                permissionEntity.Candelete = permission.CanDelete;
                _db.Permissions.Update(permissionEntity);
            }
            else
            {
                var newPermission = new Permission
                {
                    Roleid = roleId,
                    Menuid = permission.MenuId,
                    Canview = permission.CanView,
                    Canaddedit = permission.CanAddEdit,
                    Candelete = permission.CanDelete
                };
                await _db.Permissions.AddAsync(newPermission);
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction("Roles");
    }

    [HttpGet("Menu")]
    public async Task<IActionResult> Menu(int categoryid = 1)
    {
        var categories = await _menuRepository.GetCategoriesAsync();
        var items = await _menuRepository.GetItemsByCategoryAsync(categoryid);
        var modifiers = await _menuRepository.GetModifierGroupsAsync();

        var viewmodel = new MenuItemViewModel
        {
            Categories = categories,
            Items = items,
            Modifiers = modifiers
        };

        return View(viewmodel);
    }

    // [HttpGet("GetItemsByCategory")]
    // public IActionResult GetItemsByCategory(int categoryid = 1)
    // {
    //     var items = _db.Menuitemsandmodifiers
    //                     .Where(c => c.Categoryid == categoryid && c.Isdeleted == false && c.Ismodifier == false)
    //                     .ToList();

    //     var itemViewModels = items.Select(item => new ItemViewModel
    //     {
    //         Itemid = item.Itemid,
    //         Name = item.Name,
    //         Itemtype = item.Itemtype,
    //         Rate = item.Rate ?? 0,
    //         Quantity = item.Quantity,
    //         Itemimage = item.Itemimage,
    //         Isavailable = item.Isavailable
    //     }).ToList();

    //     return PartialView("_MenuItems", itemViewModels);
    // }

    [HttpGet("GetItemsByCategory")]
    public async Task<IActionResult> GetItemsByCategoryAsync(int categoryId = 1, int page = 1, int pageSize = 2, string searchTerm = "")
    {
        var (items, totalItems) = await _menuRepository.GetItemsByCategoryAsync(categoryId, page, pageSize, searchTerm);

        Console.WriteLine($" Fetching Page: {page}, PageSize: {pageSize}, Total Items: {totalItems}, Search Term: {searchTerm}");
        Console.WriteLine($" Items Retrieved: {items.Count}");

        var menuViewModel = new MenuItemViewModel
        {
            Items = items,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
            PageSize = pageSize,
            SelectedCategoryId = categoryId
        };

        return PartialView("_MenuItems", menuViewModel);
    }

    [HttpGet("GetAllModifierItems")]
    public async Task<IActionResult> GetAllModifierItemsAsync()
    {
        var items = await _menuRepository.GetAllModifierItemsAsync();
        return PartialView("_ModifierItemForModal", items);
    }


    // [HttpGet("GetModifierItemsByModifierGroup")]
    // public IActionResult GetModifierItemsByModifierGroup(int categoryid)
    // {
    //     var items = _db.Itemmodifiergroupmappings
    //     .Where(c => c.Modifiergroupid == categoryid)
    //     .Join(_db.Menuitemsandmodifiers,
    //         mapping => mapping.Itemid,
    //         item => item.Itemid,
    //         (mapping, item) => new
    //         {
    //             ItemId = item.Itemid,
    //             Name = item.Name,
    //             Unit = item.Unit,
    //             Rate = item.Rate,
    //             Quantity = item.Quantity,
    //             Isdeleted = item.Isdeleted
    //         })
    //         .Where(item => item.Isdeleted == false)
    //         .ToList();
    //     var itemViewModels = items.Select(item => new ItemViewModel
    //     {
    //         Itemid = item.ItemId,
    //         Name = item.Name,
    //         Rate = item.Rate ?? 0,
    //         Quantity = item.Quantity,
    //         Unit = item.Unit
    //     }).ToList();
    //     return PartialView("_ModifierItem", itemViewModels);
    // }

    [HttpGet("GetModifierItemsByModifierGroup")]
    public async Task<IActionResult> GetModifierItemsByModifierGroupAsync(int categoryid, string searchTerm = "", int page = 1, int pageSize = 2)
    {
        var model = await _menuRepository.GetModifierItemsByModifierGroupAsync(categoryid, searchTerm, page, pageSize);
        return PartialView("_ModifierItem", model);
    }


    [HttpPost("AddModifierItem")]
    public async Task<IActionResult> AddModifierItemAsync(Menuitemsandmodifier model, List<int> Categoryid)
    {
        if (ModelState.IsValid)
        {
            bool success = await _menuRepository.AddModifierItemAsync(model, Categoryid);

            if (success)
            {
                TempData["success"] = "Item added successfully!";
                return Json(new { success = true, message = "Item added successfully!" });
            }
        }

        return Json(new { success = false, message = "There was an error with your submission." });
    }

    //Add Modifier groups get method
    [HttpGet("AddModifierGroup")]
    public IActionResult AddModifierGroup()
    {
        return View();
    }

    //Add Modifier groups post method
    // [HttpPost("AddModifierGroup")]
    // public async Task<IActionResult> AddModifierGroup(ModifierGroupViewModel model)
    // {
    //     if (ModelState.IsValid)
    //     {
    //         try
    //         {
    //             var Modifiers = new Modifiergoup
    //             {
    //                 Groupid = model.Groupid,
    //                 Name = model.Name,
    //                 Description = model.Description,
    //                 Isdeleted = false
    //             };
    //             await _menuRepository.AddModifiersAsync(Modifiers);
    //             TempData["success"] = "Category is Added!";
    //             return RedirectToAction("Menu");
    //         }
    //         catch (Exception ex)
    //         {
    //             TempData["error"] = "An error occurred while adding the category. Please try again.";
    //             return View(model);
    //         }
    //     }
    //     return View(model);
    // }

    [HttpPost("AddModifierGroup")]
    public async Task<IActionResult> AddModifierGroup(ModifierGroupViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var modifierGroup = new Modifiergoup
                {
                    Groupid = model.Groupid,
                    Name = model.Name,
                    Description = model.Description,
                    Isdeleted = false
                };

                bool groupAdded = await _menuRepository.AddModifiers(modifierGroup);
                if (!groupAdded)
                {
                    TempData["error"] = "Failed to add modifier group.";
                    return Json(new { success = false, message = "Failed to add Modifier Group." });
                }

                if (!string.IsNullOrEmpty(model.SelectedItemIds))
                {
                    var itemIds = model.SelectedItemIds
                        .Split(',')
                        .Select(id => int.TryParse(id, out var result) ? result : 0)
                        .Where(id => id > 0)
                        .ToArray();

                    foreach (var itemId in itemIds)
                    {
                        var item = await _menuRepository.GetMenuItemByIdAsync(itemId);

                        if (item != null)
                        {
                            var modifierItem = new Itemmodifiergroupmapping
                            {
                                Modifiergroupid = modifierGroup.Groupid,
                                Itemid = item.Itemid,
                            };

                            await _menuRepository.AddModifierItem(modifierItem);
                        }
                        else
                        {
                            TempData["error"] = $"Item with ID {itemId} not found.";
                            return Json(new { success = false, message = $"Item with ID {itemId} not found." });
                        }
                    }
                }

                TempData["success"] = "Modifier Group Added Successfully!";
                return Json(new { success = true, message = "Modifier Group Added Successfully" });
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while adding the Modifier Group.";
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        TempData["error"] = "Invalid data submitted.";
        return Json(new { success = false, message = "Please check the provided data." });
    }

    //Get Method of Edit CAtegory
    [HttpGet("EditModifiers")]
    public async Task<IActionResult> EditModifiers(int id)
    {
        var modifierGroup = await _menuRepository.GetModifierGroupAsync(id);

        if (modifierGroup == null)
        {
            return NotFound();
        }

        return View(modifierGroup);
    }


    [HttpPost("EditModifiers")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditModifiersAsync(ModifierGroupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            bool updated = await _menuRepository.UpdateModifierGroupAsync(model);
            if (!updated)
            {
                TempData["error"] = "Modifier Group not found or update failed.";
                return RedirectToAction("Menu");
            }

            TempData["success"] = "Modifier Group updated successfully!";
            return RedirectToAction("Menu");
        }
        catch (Exception ex)
        {
            TempData["error"] = "An error occurred while editing the Modifier. Please try again.";
            return RedirectToAction("Menu");
        }
    }

    [HttpGet("GetModifierItemById")]
    public async Task<IActionResult> GetModifierItemByIdAsync(int itemId)
    {
        var item = await _menuRepository.GetModifierItemByIdAsync(itemId);

        if (item == null)
        {
            return NotFound();
        }

        return Json(item);
    }

    [HttpPost("EditModifierItem")]
    public async Task<IActionResult> EditModifierItemAsync(Menuitemsandmodifier item)
    {
        try
        {
            var success = await _menuRepository.EditModifierItemAsync(item);

            if (!success)
            {
                return Json(new { success = false, message = "Item not found." });
            }

            TempData["success"] = "Item Edited";
            return Json(new { success = true, message = "Modifier item updated successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred: " + ex.Message });
        }
    }


    [HttpPost("DeleteModifiersGroup")]
    public IActionResult DeleteModifiersGroup(int Id)
    {
        _menuRepository.DeleteModifierGrp(Id);
        TempData["success"] = "Modifiers is Deleted";
        return Json(new { success = true, message = "Modifier Group Deleted successfully." });
    }

    [HttpGet("AddCategory")]
    public IActionResult AddCategory()
    {
        return View();
    }

    [HttpPost("AddCategory")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var category = new Menucategory
                {
                    Categoryid = model.Categoryid,
                    Name = model.Name,
                    Description = model.Description
                };
                await _menuRepository.AddCategoriesAsync(category);
                TempData["success"] = "Category is Added!";
                return RedirectToAction("Menu");
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while adding the category. Please try again.";
                return View(model);
            }
        }
        if (model.Name == "")
        {
            ModelState.AddModelError("nameEmpty", "Name is Required");
            return View(model);
        }
        return View(model);
    }

    //Get Method of Edit CAtegory
    [HttpGet("EditCategory")]
    public IActionResult EditCategory(int id)
    {
        var category = _menuRepository.GetCategory(id);
        if (category == null)
        {
            return NotFound();
        }
        var categoryViewModel = new CategoryViewModel
        {
            Categoryid = category.Categoryid,
            Name = category.Name,
            Description = category.Description
        };
        return View(categoryViewModel);
    }

    // post method of Edit Category
    [HttpPost("EditCategory")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        bool result = await _menuRepository.EditCategoryAsync(model);

        if (result)
        {
            TempData["success"] = "Category Edited Successfully!";
        }
        else
        {
            TempData["error"] = "Category not found.";
        }

        return RedirectToAction("Menu");
    }

    // Delete  Category
    [HttpGet("DeleteCategory")]
    public IActionResult DeleteCategory(int categoryId)
    {
        _menuRepository.DeleteCategories(categoryId);
        TempData["success"] = "Category is Deleted";
        return RedirectToAction("Menu");
    }

    //Post Method of Add Item In Menu
    [HttpPost("AddNewItem")]
    public async Task<IActionResult> AddNewItem(ItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        bool result = await _menuRepository.AddNewItemAsync(model);

        if (result)
        {
            TempData["success"] = "Menu item added successfully!";
        }
        else
        {
            TempData["error"] = "An error occurred while adding the item. Please try again.";
        }

        return RedirectToAction("Menu");
    }

    //Get Method of Edit Menu Item
    [HttpGet("EditMenuItem")]
    public IActionResult EditMenuItem(int id)
    {
        try
        {
            var item = _db.Menuitemsandmodifiers
                          .Where(m => m.Itemid == id)
                          .FirstOrDefault();

            if (item == null)
            {
                return NotFound();
            }

            var categories = _db.Menucategories
                                .Where(m => m.Isdeleted == false)
                                .Select(c => new CategoryViewModel
                                {
                                    Categoryid = c.Categoryid,
                                    Name = c.Name
                                }).ToList();

            var viewModel = new MenuItemViewModel
            {
                Items = new List<ItemViewModel>
                {
                    new ItemViewModel
                    {
                        Itemid = item.Itemid,
                        Name = item.Name,
                        CategoryId = item.Categoryid,
                        Itemtype = item.Itemtype,
                        Rate = item.Rate,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        Isavailable = item.Isavailable,
                        Tax = item.Tax,
                        ItemShortCode = item.Itemshortcode,
                        Description = item.Description,
                        Itemimage = item.Itemimage,
                        // ItemPhoto = _userRepository.RetrieveImageAsIFormFile(item.Itemimage)
                    }
                },
                Categories = categories
            };
            return PartialView("_EditMenuItemModal", viewModel);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error.");
        }
    }


    // POST: Edit Menu Item (update the item)
    [HttpPost("EditMenuItemAsync")]
    public async Task<IActionResult> EditMenuItemAsync(int id, MenuItemViewModel model)
    {
        try
        {
            var success = await _menuRepository.UpdateMenuItemAsync(id, model);
            if (!success)
            {
                return NotFound();
            }

            return Json(new { success = true, message = "Item updated successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("DeleteMenuItem")]
    public IActionResult DeleteMenuItem(int itemId)
    {
        // Your logic to delete the item from the database
        var item = _db.Menuitemsandmodifiers.FirstOrDefault(i => i.Itemid == itemId);
        if (item != null)
        {
            item.Isdeleted = true;
            _db.SaveChanges();
        }
        return RedirectToAction("Menu");
    }

    //mass delete in menu item
    [HttpPost("MassDeleteMenuItem")]
    public IActionResult MassDeleteMenuItem(List<int> itemIds)
    {
        if (itemIds == null || !itemIds.Any())
        {
            return Json(new { success = false, message = "No items selected." });
        }
        try
        {
            foreach (var itemId in itemIds)
            {
                var item = _db.Menuitemsandmodifiers.Find(itemId);
                if (item != null)
                {
                    item.Isdeleted = true;
                }
            }
            _db.SaveChanges();
            TempData["success"] = "Items deleted successful";
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
