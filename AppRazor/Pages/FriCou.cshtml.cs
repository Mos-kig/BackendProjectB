using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models.Interfaces;
using Services;
using Services.Interfaces;

namespace AppRazor.Pages
{
    public class FriCouModel : PageModel
    {
        public readonly IAdminService _adminService;
        public IEnumerable<IGrouping<string, Models.DTO.GstUsrInfoFriendsDto>>? CountryInfo;

        public async Task<IActionResult> OnGet()
        {
            var info = await _adminService.GuestInfoAsync();
            CountryInfo = info.Item.Friends.GroupBy(f => f.Country);
            return Page();
        }
        public FriCouModel(IAdminService adminService)
        {
            _adminService = adminService;
        }
    }
}
