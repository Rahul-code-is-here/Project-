using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BLL.IRepository;
using DAL.Database;
using DAL.Models;
using DAL.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Org.BouncyCastle.Asn1.Ocsp;

namespace BLL.Repository;

public class UserRepository : IUserRepository
{
    private readonly PizzaShopDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ITokenRepository _tokenRepository;
    private readonly IEmailSender _emailSender;

    public UserRepository(PizzaShopDbContext db, ITokenRepository tokenRepository, IEmailSender emailSender, IWebHostEnvironment env) //, ITokenService tokenService
    {
        _db = db;
        _tokenRepository = tokenRepository;
        _emailSender = emailSender;
        _env = env;
    }

    public bool ValidateUserCredentials(string email, string password)
    {
        var existingUser = _db.Logins.FirstOrDefault(u => u.Email == email.ToLower());
        if (existingUser != null)
        {
            return BCrypt.Net.BCrypt.Verify(password, existingUser.Password);
        }
        return false;
    }

    public string GenerateJwtToken(string email)
    {
        var existingUser = _db.Logins.FirstOrDefault(u => u.Email == email.ToLower());
        if (existingUser != null)
        {
            var role = _db.Roles.FirstOrDefault(r => r.Roleid == existingUser.Roleid);
            string roleName = role != null ? role.Rolename : "User";
            return _tokenRepository.GenerateJwtToken(existingUser.Email, roleName, roleName);
        }
        return null;
    }

    public TEntity GetEntityByField<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
    {
        return _db.Set<TEntity>().FirstOrDefault(predicate);
    }

    public Login GetLoginUserDetails(string email)
    {
        return GetEntityByField<Login>(u => u.Email.ToLower() == email.ToLower());
    }

    public Usertable GetUserDetails(string email)
    {
        return GetEntityByField<Usertable>(u => u.Email == email.ToLower());
    }

    public string GetUserEmailFromJwtToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return string.Empty;
        }

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(p => p.Type == ClaimTypes.Name)?.Value ?? "";
    }

    public Usertable GetUser(string email)
    {
        return _db.Usertables.FirstOrDefault(u => u.Email == email.ToLower());
    }

    public UsertableViewModel FetchUserDetail(Usertable user)
    {
        var objPass = new UsertableViewModel()
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Phone = user.Phone,
            Country = user.Country,
            State = user.State,
            City = user.City,
            Address = user.Address,
            Zipcode = user.Zipcode,
            Email = user.Email,
            Images = user.Images
        };
        return objPass;
    }

    public string UpdateUser(UsertableViewModel model, Usertable user)
    {
        var countryname = _db.Countries.FirstOrDefault(c => c.Countryid.ToString() == model.Country);
        var statename = _db.States.FirstOrDefault(c => c.Stateid.ToString() == model.State);
        var cityname = _db.Cities.FirstOrDefault(c => c.Cityid.ToString() == model.City);
        if (countryname != null)
        {
            model.Country = countryname?.Countryname;
        }
        if (statename != null)
        {
            model.State = statename?.Statename;
        }
        if (cityname != null)
        {
            model.City = cityname?.Cityname;
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.Email = model.Email.ToLower().Trim();
        user.Username = model.Username.Trim();
        user.Phone = model.Phone;
        user.Country = model.Country;
        user.State = model.State;
        user.City = model.City;
        user.Address = model.Address;
        user.Zipcode = model.Zipcode;
        user.Images = ImageUpload(model.profile);

        _db.SaveChanges();
        return "Update Successful!!!";
    }

    public string ChangePassword(Login loginuser, Usertable user, string newPasswordHash)
    {
        loginuser.Password = newPasswordHash;
        user.Password = newPasswordHash;
        _db.Logins.Update(loginuser);
        _db.Usertables.Update(user);
        _db.SaveChanges();
        return "PassWord change done successfully";
    }
    public async Task<(List<UsersViewModel> users, int totalUsers, int totalPages)> GetPaginatedUsersAsync(string email,
    string searchTerm, int page, int itemsPerPage, string sortColumn = "Name", string sortOrder = "asc")
    {
        var usersQuery = _db.Usertables.Where(u => u.Isdeleted == false && u.Email != email).AsQueryable();
        if (!string.IsNullOrEmpty(searchTerm))
        {
            usersQuery = usersQuery.Where(u => u.Username.ToLower().Contains(searchTerm.ToLower()));
        }
        var totalUsers = await usersQuery.CountAsync();
        // Sorting logic
        usersQuery = sortColumn.ToLower() switch
        {
            "name" => (sortOrder == "asc") ? usersQuery.OrderBy(u => u.Username) : usersQuery.OrderByDescending(u => u.Username),
            "role" => (sortOrder == "asc") 
                ? usersQuery.OrderBy(u => u.Role.Rolename)  
                : usersQuery.OrderByDescending(u => u.Role.Rolename),
            _ => usersQuery.OrderBy(u => u.Username) // Default sorting
        };
        var userPagination = await usersQuery
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .Join(_db.Roles, u => u.Roleid, r => r.Roleid, (u, r) => new UsersViewModel
            {
                Name = u.Username,
                Email = u.Email,
                Phone = u.Phone,
                Roleid = r.Roleid,
                RoleName = r.Rolename,
                Status = u.Status,
                Images = u.Images
            })
            .ToListAsync();
        var totalPages = (int)Math.Ceiling((double)totalUsers / itemsPerPage);
        return (userPagination, totalUsers, totalPages);
    }

    public void SetCountryStateCity(AddUserViewModel model)
    {
        var countryname = _db.Countries.FirstOrDefault(c => c.Countryid.ToString() == model.Country);
        var statename = _db.States.FirstOrDefault(c => c.Stateid.ToString() == model.State);
        var cityname = _db.Cities.FirstOrDefault(c => c.Cityid.ToString() == model.City);

        model.Country = countryname?.Countryname;
        model.State = statename?.Statename;
        model.City = cityname?.Cityname;
    }

    public Role GetRole(AddUserViewModel model)
    {
        return _db.Roles.FirstOrDefault(r => r.Rolename == model.Rolename);
    }

    public string AddUser(Role role, AddUserViewModel model)
    {
        if (model.profile == null)
        {
            model.profile = RetrieveImageAsIFormFile("userimages/userprofile/c6fd910f-aeb2-40d2-af87-b5ded020ba6e.png");
        }
        var hashpass = BCrypt.Net.BCrypt.HashPassword(model.Password);
        var user = new Usertable
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email.ToLower().Trim(),
            Username = model.Username.Trim(),
            Phone = model.Phone,
            Password = hashpass,
            Roleid = role.Roleid,
            Country = model.Country,
            State = model.State,
            City = model.City,
            Address = model.Address,
            Zipcode = model.Zipcode,
            Images = ImageUpload(model.profile)
        };
        var loginuser = new Login
        {
            Email = model.Email.Trim().ToLower(),
            Password = hashpass,
            Roleid = role.Roleid
        };

        _db.Logins.Add(loginuser);
        _db.Usertables.Add(user);
        _db.SaveChanges();
        // ------Email send------------

        string emailfilePath = @"C:\Users\pca120\Documents\Project\RMS\PizzaShop\EmailFormet\AddUser.html";

        string emailBody = System.IO.File.ReadAllText(emailfilePath);

        emailBody = emailBody.Replace("{username}", model.Email);
        emailBody = emailBody.Replace("{password}", model.Password);

        _emailSender.SendEmailAsync(model.Email, "User Details", "add user", emailBody);
        return "User was added !!!!";
    }

    public Role GetRole(Usertable user)
    {
        return _db.Roles.FirstOrDefault(r => r.Roleid == user.Roleid);
    }

    public EditUserViewModel GetDataforEditPage(Usertable user, Role role)
    {
        var objpass = new EditUserViewModel
        {
            FirstName = user.FirstName.Trim(),
            LastName = user.LastName.Trim(),
            Username = user.Username.Trim(),
            Rolename = role.Rolename,
            Email = user.Email.Trim().ToLower(),
            Status = user.Status.Value,
            Country = user.Country,
            State = user.State,
            City = user.City,
            Zipcode = user.Zipcode,
            Address = user.Address,
            Phone = user.Phone
        };
        return objpass;
    }

    public string EditUser(Usertable user, EditUserViewModel model)
    {
        var countryname = _db.Countries.FirstOrDefault(c => c.Countryid.ToString() == model.Country);
        var statename = _db.States.FirstOrDefault(c => c.Stateid.ToString() == model.State);
        var cityname = _db.Cities.FirstOrDefault(c => c.Cityid.ToString() == model.City);
        if (countryname != null)
        {
            model.Country = countryname?.Countryname;
        }
        if (statename != null)
        {
            model.State = statename?.Statename;
        }
        if (cityname != null)
        {
            model.City = cityname?.Cityname;
        }
        if (model.profile == null)
        {
            model.profile = RetrieveImageAsIFormFile(user.Images);
        }
        var role = _db.Roles.FirstOrDefault(r => r.Rolename == model.Rolename);
        if (role == null)
        {
            role = _db.Roles.FirstOrDefault(r => r.Roleid.ToString() == model.Rolename);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.Username = model.Username.Trim();
        user.Roleid = role.Roleid;
        user.Status = model.Status;
        user.Country = model.Country;
        user.State = model.State;
        user.City = model.City;
        user.Zipcode = model.Zipcode;
        user.Phone = model.Phone;
        user.Images = ImageUpload(model.profile);

        _db.SaveChanges();
        return "User Edited";
    }
    public string ImageUpload(IFormFile pic)
    {

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

        string fileExtension = Path.GetExtension(pic.FileName).ToLower();

        if (!allowedExtensions.Contains(fileExtension))
        {
            return "Invalid file format. Only JPG, JPEG, PNG, and GIF files are allowed.";
        }
        string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/userimages/userprofile");
        if (!Directory.Exists(uploadDir))
        {
            Directory.CreateDirectory(uploadDir);
        }
        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(pic.FileName);
        string filePath = Path.Combine(uploadDir, fileName);
        // Save the file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            pic.CopyTo(stream);
        }
        // Store the relative path
        string imagePath = "userimages/userprofile/" + fileName;

        return imagePath;
    }
    public IFormFile RetrieveImageAsIFormFile(string imagePath)
    {
        string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath);

        if (File.Exists(uploadDir))
        {
            byte[] fileBytes = File.ReadAllBytes(uploadDir);
            var stream = new MemoryStream(fileBytes);

            var formFile = new FormFile(stream, 0, stream.Length, "file", Path.GetFileName(uploadDir))
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            return formFile;
        }
        else
        {
            throw new FileNotFoundException("Image not found at the specified path.");
        }
    }

    public List<Role> GetRoles(){
        return _db.Roles.ToList();
    }

    public string GetRoleName(int roleId)
    {
        return _db.Roles.Where(r => r.Roleid == roleId).Select(u => u.Rolename).FirstOrDefault();
    }
}
