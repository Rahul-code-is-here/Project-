using DAL.Models;
using DAL.ViewModels;
using Microsoft.AspNetCore.Http;

namespace BLL.IRepository;

public interface IUserRepository
{
    bool ValidateUserCredentials(string email, string password);
    string GenerateJwtToken(string email);
    Login GetLoginUserDetails(string email);
    Usertable GetUserDetails(string email);

    public TEntity GetEntityByField<TEntity>(Func<TEntity, bool> predicate) where TEntity : class;

    string GetUserEmailFromJwtToken(string email);

    Usertable GetUser(string email);

    UsertableViewModel FetchUserDetail(Usertable user);

    string UpdateUser(UsertableViewModel model, Usertable user);

    string ChangePassword(Login loginuser, Usertable user, string newPasswordHash);
    Task<(List<UsersViewModel> users, int totalUsers, int totalPages)> GetPaginatedUsersAsync(string email, string searchTerm, int page, int itemsPerPage, string sortColumn = "Name", string sortOrder = "asc");
    void SetCountryStateCity(AddUserViewModel model);
    Role GetRole(AddUserViewModel model);
    string AddUser(Role role, AddUserViewModel model);
    Role GetRole(Usertable user);
    EditUserViewModel GetDataforEditPage(Usertable user,Role role);

    string EditUser(Usertable user, EditUserViewModel model);
    string ImageUpload(IFormFile pic);
    IFormFile RetrieveImageAsIFormFile(string imagePath);
    List<Role> GetRoles();

    string GetRoleName(int roleId);
}

