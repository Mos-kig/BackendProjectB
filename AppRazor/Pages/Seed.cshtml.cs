using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services;
using Services.Interfaces;

namespace AppRazor.Pages;

public class SeedModel : PageModel
{
    readonly IAdminService _admin_service;
    readonly ILogger<SeedModel> _logger;
    public int NrOfGroups => nrOfGroups().Result;
    private async Task<int> nrOfGroups()
    {
        var info = await _admin_service.GuestInfoAsync();
        return info.Item.Db.NrSeededFriends + info.Item.Db.NrUnseededFriends;
    }

    [BindProperty]
    [Required(ErrorMessage = "You must enter nr of items to seed")]
    public int NrOfItemsToSeed { get; set; } = 100;

    [BindProperty]
    public bool RemoveSeeds { get; set; } = true;

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (ModelState.IsValid)
        {
            if (RemoveSeeds)
            {
                await _admin_service.RemoveSeedAsync(true);
                await _admin_service.RemoveSeedAsync(false);
            }
            await _admin_service.SeedAsync(NrOfItemsToSeed);

            return Redirect($"~/FriCou");
        }
        return Page();
    }

    public SeedModel(IAdminService admin_service, ILogger<SeedModel> logger)
    {
        _admin_service = admin_service;
        _logger = logger;
    }
}
