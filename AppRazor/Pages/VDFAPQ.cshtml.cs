using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using Services.Interfaces;

namespace AppRazor.Pages
{
    public class VDFAPQModel : PageModel
    {
        readonly IFriendsService _service = null;
        readonly ILogger<VDFAPQModel> _logger = null;
        public csFriend Friend { get; set; }

        public string ErrorMessage { get; set; } = null;

        public async Task<IActionResult> OnGet(string id)
        {
            try
            {
                Guid _id = Guid.Parse(id);
                var response = await _service.ReadFriendAsync(_id, false);
                Friend = response.Item as csFriend;
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
            }
            return Page();
        }
        public VDFAPQModel(IFriendsService service, ILogger<VDFAPQModel> logger)
        {
            _logger = logger;
            _service = service;
        }
    }
}
