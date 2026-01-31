using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using Services.Interfaces;

namespace AppRazor.Pages
{
    public class FriCouModel : PageModel
    {
        public readonly IFriendsService _friendsService;
        public IEnumerable<IGrouping<string, csFriend>>? CountryInfo;

        public async Task<IActionResult> OnGet()
        {
            var response = await _friendsService.ReadFriendsAsync(true, false, "", 0, int.MaxValue);
            var allFriends = response.PageItems.Cast<csFriend>().ToList();

            CountryInfo = allFriends.GroupBy(f => f.Address?.Country ?? "Unknown Country")
                .OrderBy(g => g.Key == "Unknown Country" ? "zzz" : g.Key);
            return Page();
        }

        public FriCouModel(IFriendsService friendsService)
        {
            _friendsService = friendsService;
        }
    }
}
