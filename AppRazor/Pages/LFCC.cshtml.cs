using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using Services.Interfaces;

namespace AppRazor.Pages
{
    public class LFCCModel : PageModel
    {
        public readonly IFriendsService _friendsService;
        readonly ILogger<LFCCModel> _logger = null;

        public List<csFriend> friends { get; set; } = new List<csFriend>();

        public string ErrorMessage { get; set; } = null;

        //Pagination
        public int NrOfPages { get; set; }
        public int PageSize { get; } = 5;

        public int ThisPageNr { get; set; } = 0;
        public int PrevPageNr { get; set; } = 0;
        public int NextPageNr { get; set; } = 0;
        public int PresentPages { get; set; } = 0;

        //ModelBinding for the form
        [BindProperty]
        public string SearchFilter { get; set; }

        public async Task<IActionResult> OnGetDetails(string id)
        {
            try
            {
                Guid _id = Guid.Parse(id);
                //Use the Service
                var response = await _friendsService.ReadFriendAsync(_id, false);
                friends = new List<csFriend> { (csFriend)response.Item };
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
            }
            return Page();
        }
        public async Task<IActionResult> OnGet(string pagenr, string search)
        {
            //Read a QueryParameters
            if (int.TryParse(pagenr, out int _pagenr))
            {
                ThisPageNr = _pagenr;
            }

            SearchFilter = Request.Query["search"];

            //Pagination
            await UpdatePaginationAsync();

            //Use the Service
            var response = await _friendsService.ReadFriendsAsync(true, false, SearchFilter, ThisPageNr, PageSize);
            friends = response.PageItems.Cast<csFriend>().ToList();
            return Page();
        }
        private async Task UpdatePaginationAsync()
        {
            //Pagination
            var response = await _friendsService.ReadFriendsAsync(true, false, SearchFilter, 0, int.MaxValue);
            NrOfPages = (int)Math.Ceiling((double)response.DbItemsCount / PageSize);
            PrevPageNr = Math.Max(0, ThisPageNr - 1);
            NextPageNr = Math.Min(NrOfPages - 1, ThisPageNr + 1);
            PresentPages = Math.Min(3, NrOfPages);
        }
        public async Task<IActionResult> OnPostSearch()
        {
            //Pagination
            await UpdatePaginationAsync();

            //Use the Service 
            var response = await _friendsService.ReadFriendsAsync(true, false, SearchFilter, ThisPageNr, PageSize);
            friends = response.PageItems.Cast<csFriend>().ToList();
            //Page is rendered as the postback is part of the form tag
            return Page();
        }
        public LFCCModel(IFriendsService friendsService, ILogger<LFCCModel> logger)
        {
            _friendsService = friendsService;
            _logger = logger;
        }
    }
}
