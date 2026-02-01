using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using Models.DTO;
using Services.Interfaces;
using WebAppStudies.SeidoHelpers;
namespace AppRazor.Pages
{
    public class VDFAPQModel : PageModel
    {
        readonly IFriendsService _service;
        readonly ILogger<VDFAPQModel> _logger;
        public csFriend Friend { get; set; }
        public string ErrorMessage { get; set; } = null;

        [BindProperty]
        public List<FriendIM> friendIM { get; set; } = new List<FriendIM>();

        public ModelValidationResult ValidationResult { get; set; } = new ModelValidationResult(false, null, null);

        public async Task<IActionResult> OnGet(string id)
        {
            try
            {
                Guid _id = Guid.Parse(id);
                var response = await _service.ReadFriendAsync(_id, false);
                Friend = response.Item as csFriend;
                friendIM = Friend != null ? new List<FriendIM> { new FriendIM(Friend) } : new List<FriendIM>();
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
            }
            return Page();
        }
        public async Task<IActionResult> OnPostDelete(Guid friendId)
        {
            // Get friend ID from multiple sources if the parameter is empty
            if (friendId == Guid.Empty)
            {
                string formFriendId = Request.Form["currentFriendId"];
                if (!string.IsNullOrEmpty(formFriendId) && Guid.TryParse(formFriendId, out Guid parsedId))
                {
                    friendId = parsedId;
                }
                else if (friendIM?.Count > 0)
                {
                    friendId = friendIM.First().FriendId;
                }
            }

            try
            {
                if (friendId == Guid.Empty)
                {
                    ErrorMessage = "Invalid friend ID for deletion.";
                    return Page();
                }

                await _service.DeleteFriendAsync(friendId);

                // Clear the current friend data to show it's been deleted
                Friend = null;
                friendIM = new List<FriendIM>();

                ErrorMessage = "Friend deleted successfully.";
            }
            catch (Exception e)
            {
                ErrorMessage = $"Error deleting friend: {e.Message}";

                // Try to reload the friend data if deletion failed
                try
                {
                    var response = await _service.ReadFriendAsync(friendId, false);
                    Friend = response.Item as csFriend;
                    friendIM = Friend != null ? new List<FriendIM> { new FriendIM(Friend) } : new List<FriendIM>();
                }
                catch (Exception reloadEx)
                {
                    ErrorMessage += $" Error reloading friend data: {reloadEx.Message}";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostEdit(Guid friendId)
        {
            // Use form friend ID if the parameter is empty
            Guid actualFriendId = friendId;
            if (friendId == Guid.Empty && friendIM.Any())
            {
                actualFriendId = friendIM[0].FriendId;
            }
            // Validate we have a valid friend ID
            if (actualFriendId == Guid.Empty)
            {
                ErrorMessage = "Invalid friend ID.";
                return Page();
            }

            // Validate the input
            string[] keys = {
                "friendIM[0].EditFirstName",
                "friendIM[0].EditLastName",
                "friendIM[0].EditEmail"
            };

            // Perform partial model validation
            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }

            // Get the submitted values from the model-bound property
            var submittedFriend = friendIM[0];

            // Save to database immediately
            try
            {

                var response = await _service.ReadFriendAsync(actualFriendId, false);
                var model = response.Item as csFriend;

                if (model == null)
                {
                    ErrorMessage = "Friend not found in database.";
                    return Page();
                }

                // Update with the new values from the bound model
                model.FirstName = submittedFriend.EditFirstName;
                model.LastName = submittedFriend.EditLastName;
                model.Email = submittedFriend.EditEmail;

                // Parse birthday if provided
                if (submittedFriend.EditBirthday.HasValue)
                {
                    model.Birthday = submittedFriend.EditBirthday;
                }
                else
                {
                    model.Birthday = null;
                }

                var updateDto = new FriendCuDto(model);
                var updateResult = await _service.UpdateFriendAsync(updateDto);

                // Force a fresh read from database to verify the update
                var verifyResponse = await _service.ReadFriendAsync(actualFriendId, false);
                var verifiedFriend = verifyResponse.Item as csFriend;
                Friend = verifiedFriend; // Update the Friend property
                friendIM = verifiedFriend != null ? new List<FriendIM> { new FriendIM(verifiedFriend) } : new List<FriendIM>();
            }
            catch (Exception e)
            {
                ErrorMessage = $"Error saving changes: {e.Message}";

                // Try to reload the page data
                try
                {
                    var reloadResponse = await _service.ReadFriendAsync(actualFriendId, false);
                    Friend = reloadResponse.Item as csFriend;
                    if (Friend != null)
                    {
                        friendIM = new List<FriendIM> { new FriendIM(Friend) };
                    }
                }
                catch (Exception reloadEx)
                {
                    ErrorMessage += $" Error reloading friend data: {reloadEx.Message}";
                }
                return Page();
            }

            return Page();
        }
        public async Task<IActionResult> OnPostUndo()
        {
            try
            {
                // Get the friend ID from multiple sources
                Guid friendId = Friend?.FriendId ?? Guid.Empty;

                // Try form data first
                if (friendId == Guid.Empty)
                {
                    string formFriendId = Request.Form["currentFriendId"];
                    if (!string.IsNullOrEmpty(formFriendId) && Guid.TryParse(formFriendId, out Guid parsedFormId))
                    {
                        friendId = parsedFormId;
                    }
                }

                // Try friendIM collection
                if (friendId == Guid.Empty && friendIM?.Count > 0)
                {
                    friendId = friendIM.First().FriendId;
                }

                // Try query string as last resort
                if (friendId == Guid.Empty)
                {
                    string idParam = Request.Query["id"];
                    if (!string.IsNullOrEmpty(idParam) && Guid.TryParse(idParam, out Guid parsedId))
                    {
                        friendId = parsedId;
                    }
                }

                if (friendId == Guid.Empty)
                {
                    ErrorMessage = "Unable to determine friend ID for reload.";
                    return Page();
                }

                // Reload the friend data from the database
                var response = await _service.ReadFriendAsync(friendId, false);
                Friend = response.Item as csFriend;
                friendIM = new List<FriendIM> { new FriendIM(Friend) };

                ErrorMessage = null; // Clear any previous errors
            }
            catch (Exception e)
            {
                ErrorMessage = $"Error reloading data: {e.Message}";
            }
            return Page();
        }
        public VDFAPQModel(IFriendsService service, ILogger<VDFAPQModel> logger)
        {
            _logger = logger;
            _service = service;
        }

        public enum StatusIM { Unknown, Unchanged, Inserted, Modified, Deleted }

        public class FriendIM
        {
            //Status of InputModel
            public StatusIM StatusIM { get; set; }

            //Properties from Model which is to be edited in the <form>
            public Guid FriendId { get; init; } = Guid.NewGuid();
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public DateTime? Birthday { get; set; }


            //Edit properties for in-place editing
            [Required(ErrorMessage = "You must provide a FirstName")]
            public string EditFirstName { get; set; }

            [Required(ErrorMessage = "You must provide a LastName")]
            public string EditLastName { get; set; }

            [Required(ErrorMessage = "You must provide an email")]
            [RegularExpression(@"\S+", ErrorMessage = "Email cannot be empty or just whitespace")]
            public string EditEmail { get; set; }

            public DateTime? EditBirthday { get; set; }

            #region constructors and model update
            public FriendIM() { StatusIM = StatusIM.Unchanged; }

            //Copy constructor
            public FriendIM(FriendIM original)
            {
                StatusIM = original.StatusIM;

                FriendId = original.FriendId;
                FirstName = original.FirstName;
                LastName = original.LastName;
                Email = original.Email;
                Birthday = original.Birthday;

                EditFirstName = original.EditFirstName;
                EditLastName = original.EditLastName;
                EditEmail = original.EditEmail;
                EditBirthday = original.EditBirthday;
            }

            //Model => InputModel constructor
            public FriendIM(csFriend original)
            {
                if (original == null)
                {
                    StatusIM = StatusIM.Unknown;
                    return;
                }

                StatusIM = StatusIM.Unchanged;
                FriendId = original.FriendId;
                FirstName = EditFirstName = original.FirstName;
                LastName = EditLastName = original.LastName;
                Email = EditEmail = original.Email;
                Birthday = EditBirthday = original.Birthday;
            }

            //InputModel => Model
            public csFriend UpdateModel(csFriend model)
            {
                model.FriendId = FriendId;
                model.FirstName = FirstName;
                model.LastName = LastName;
                model.Email = Email;
                model.Birthday = Birthday;
                return model;
            }
            #endregion

        }
    }
}
