using System.ComponentModel.DataAnnotations;
using DAL.Models;
using Microsoft.AspNetCore.Http;

namespace DAL.ViewModels;

public class MenuItemViewModel
{
        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
        public List<ItemViewModel> Items { get; set; } = new List<ItemViewModel>();
        public List<ModifierGroupViewModel> Modifiers { get; set; } = new List<ModifierGroupViewModel>();
        // public Pagination<ItemViewModel> PaginatedItems { get; set; } = new Pagination<ItemViewModel>();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int SelectedCategoryId { get; set; }
        public IFormFile ItemPhoto { get; set; }
        public List<int> SelectedModifiers { get; set; } = new List<int>();
}
