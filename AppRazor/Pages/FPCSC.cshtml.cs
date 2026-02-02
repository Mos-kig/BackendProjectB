using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using Services.Interfaces;

namespace AppRazor.Pages
{
    public class FPCSCModel : PageModel
    {
        public readonly IFriendsService _friendsService;
        public readonly IPetsService _petsService;
        public IEnumerable<IGrouping<string, csFriend>>? CityInfo;
        public IEnumerable<IGrouping<string, Pet>>? PetInfo;

        //ModelBinding for the form
        [BindProperty(SupportsGet = true)]
        public string SearchFilter { get; set; }
        public async Task OnGetAsync()
        {
            var currentFilter = SearchFilter ?? "";

            var response = await _friendsService.ReadFriendsAsync(true, false, currentFilter, 0, int.MaxValue);
            var allFriends = response.PageItems.Cast<csFriend>().ToList();

            CityInfo = allFriends.GroupBy(f => f.Address?.City ?? "Unknown City")
                .OrderBy(g => g.Key == "Unknown City" ? "zzz" : g.Key);

            var petResponse = await _petsService.ReadPetsAsync(true, false, currentFilter, 0, int.MaxValue);
            var allPets = petResponse.PageItems.Cast<Pet>().ToList();
            PetInfo = allPets.GroupBy(p => p.Friend?.Address?.City ?? "Unknown City")
                .OrderBy(g => g.Key == "Unknown City" ? "zzz" : g.Key);
        }

        public FPCSCModel(IFriendsService friendsService, IPetsService petsService)
        {
            _friendsService = friendsService;
            _petsService = petsService;
        }
    }
}
