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
        readonly IAddressesService _addressService;
        readonly IPetsService _petsService;
        readonly IQuotesService _quotesService;
        readonly ILogger<VDFAPQModel> _logger;
        public csFriend Friend { get; set; }
        public string ErrorMessage { get; set; } = null;

        [BindProperty]
        public List<FriendIM> friendIM { get; set; } = new List<FriendIM>();

        [BindProperty]
        public List<AddressIM> AddressIMs { get; set; } = new List<AddressIM>();
        public ModelValidationResult ValidationResult { get; set; } = new ModelValidationResult(false, null, null);
        public VDFAPQModel(IFriendsService service, ILogger<VDFAPQModel> logger, IAddressesService addressService, IPetsService petsService, IQuotesService quotesService)
        {
            _logger = logger;
            _service = service;
            _addressService = addressService;
            _petsService = petsService;
            _quotesService = quotesService;
        }
        public async Task<IActionResult> OnGet(string id)
        {
            try
            {
                Guid _id = Guid.Parse(id);
                var response = await _service.ReadFriendAsync(_id, false);
                Friend = response.Item as csFriend;
                friendIM = Friend != null ? new List<FriendIM> { new FriendIM(Friend) } : new List<FriendIM>();

                if (Friend?.Address != null)
                {
                    AddressIMs = new List<AddressIM> { new AddressIM(Friend.Address) };
                }
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
                AddressIMs = new List<AddressIM>();

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
                    AddressIMs = Friend?.Address != null ? new List<AddressIM> { new AddressIM(Friend.Address) } : new List<AddressIM>();
                }
                catch (Exception reloadEx)
                {
                    ErrorMessage += $" Error reloading friend data: {reloadEx.Message}";
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeletePet(Guid itemId, Guid currentFriendId)
        {
            if (itemId == Guid.Empty)
            {
                string formItemId = Request.Form["itemId"];
                if (!string.IsNullOrEmpty(formItemId) && Guid.TryParse(formItemId, out Guid parsedId))
                {
                    itemId = parsedId;
                }
            }

            if (currentFriendId == Guid.Empty)
            {
                string formFriendId = Request.Form["currentFriendId"];
                if (!string.IsNullOrEmpty(formFriendId) && Guid.TryParse(formFriendId, out Guid parsedId))
                {
                    currentFriendId = parsedId;
                }
            }

            if (itemId == Guid.Empty || currentFriendId == Guid.Empty)
            {
                ErrorMessage = "Could not delete pet. Required ID was missing.";
                if (currentFriendId != Guid.Empty)
                {
                    return await OnGet(currentFriendId.ToString());
                }
                return Page();
            }

            try
            {
                await _petsService.DeletePetAsync(itemId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting pet with ID {PetId}", itemId);
                ErrorMessage = $"Error deleting pet: {e.Message}";
                await OnGet(currentFriendId.ToString());
                return Page();
            }

            return RedirectToPage(new { id = currentFriendId });
        }

        public async Task<IActionResult> OnPostDeleteQuote(Guid itemId, Guid currentFriendId)
        {
            if (itemId == Guid.Empty)
            {
                string formItemId = Request.Form["itemId"];
                if (!string.IsNullOrEmpty(formItemId) && Guid.TryParse(formItemId, out Guid parsedId))
                {
                    itemId = parsedId;
                }
            }

            if (currentFriendId == Guid.Empty)
            {
                string formFriendId = Request.Form["currentFriendId"];
                if (!string.IsNullOrEmpty(formFriendId) && Guid.TryParse(formFriendId, out Guid parsedId))
                {
                    currentFriendId = parsedId;
                }
            }

            if (itemId == Guid.Empty || currentFriendId == Guid.Empty)
            {
                ErrorMessage = "Could not delete quote. Required ID was missing.";
                if (currentFriendId != Guid.Empty)
                {
                    return await OnGet(currentFriendId.ToString());
                }
                return Page();
            }

            try
            {
                await _quotesService.DeleteQuoteAsync(itemId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting quote with ID {QuoteId}", itemId);
                ErrorMessage = $"Error deleting quote: {e.Message}";
                await OnGet(currentFriendId.ToString());
                return Page();
            }

            return RedirectToPage(new { id = currentFriendId });
        }


        public async Task<IActionResult> OnPostEdit(Guid friendId)
        {
            Guid actualFriendId = friendId;
            if (friendId == Guid.Empty && friendIM.Any())
            {
                actualFriendId = friendIM[0].FriendId;
            }

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
                if (Friend?.Address != null)
                {
                    AddressIMs = new List<AddressIM> { new AddressIM(Friend.Address) };
                }
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

        public async Task<IActionResult> OnPostEditAddress(Guid friendId)
        {
            // Use form friend ID if the parameter is empty
            Guid actualFriendId = friendId;
            if (friendId == Guid.Empty && friendIM.Any())
            {
                actualFriendId = friendIM[0].FriendId;
            }
            // Ensure we have a valid friendId
            if (actualFriendId == Guid.Empty)
            {
                ErrorMessage = "Invalid friend ID for address update.";
                return Page();
            }

            // Server-side validation for the address fields
            string[] keys = {
                "AddressIMs[0].EditStreetAddress",
                "AddressIMs[0].EditZipCode",
                "AddressIMs[0].EditCity",
                "AddressIMs[0].EditCountry"
            };

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                var friendResponse = await _service.ReadFriendAsync(actualFriendId, false);
                Friend = friendResponse.Item as csFriend;
                friendIM = Friend != null ? new List<FriendIM> { new FriendIM(Friend) } : new List<FriendIM>();
                return Page();
            }

            try
            {
                var submittedAddress = AddressIMs[0];
                // Create a DTO for the address update
                var addressDto = new AddressCuDto
                {
                    AddressId = submittedAddress.AddressId, // Get the ID from the hidden form field
                    StreetAddress = submittedAddress.EditStreetAddress,
                    ZipCode = submittedAddress.EditZipCode,
                    City = submittedAddress.EditCity,
                    Country = submittedAddress.EditCountry,
                    FriendsId = new List<Guid> { actualFriendId }
                };
                await _addressService.UpdateAddressAsync(addressDto);

                var verifyResponse = await _service.ReadFriendAsync(actualFriendId, false);
                Friend = verifyResponse.Item as csFriend;
                friendIM = Friend != null ? new List<FriendIM> { new FriendIM(Friend) } : new List<FriendIM>();
                if (Friend?.Address != null)
                {
                    AddressIMs = new List<AddressIM> { new AddressIM(Friend.Address) };
                }
            }
            catch (Exception e)
            {
                ErrorMessage = $"Error saving address: {e.Message}";
                var friendResponse = await _service.ReadFriendAsync(actualFriendId, false);
                Friend = friendResponse.Item as csFriend;
                friendIM = Friend != null ? new List<FriendIM> { new FriendIM(Friend) } : new List<FriendIM>();
                if (Friend?.Address != null)
                {
                    AddressIMs = new List<AddressIM> { new AddressIM(Friend.Address) };
                }
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

        public class AddressIM
        {
            //Status of InputModel
            public StatusIM StatusIM { get; set; }

            //Properties from Model which is to be edited in the <form>
            public Guid AddressId { get; set; }
            public string StreetAddress { get; set; }
            public int ZipCode { get; set; }
            public string City { get; set; }
            public string Country { get; set; }

            //Edit properties for in-place editing
            [Required(ErrorMessage = "Street Address is required")]
            public string EditStreetAddress { get; set; }

            [Required(ErrorMessage = "Zip Code is required")]
            public int EditZipCode { get; set; }

            [Required(ErrorMessage = "City is required")]
            public string EditCity { get; set; }

            [Required(ErrorMessage = "Country is required")]
            public string EditCountry { get; set; }
            public AddressIM() { StatusIM = StatusIM.Unchanged; }


            //Copy constructor
            public AddressIM(AddressIM original)
            {
                StatusIM = original.StatusIM;

                AddressId = original.AddressId;
                StreetAddress = original.StreetAddress;
                ZipCode = original.ZipCode;
                City = original.City;
                Country = original.Country;

                EditStreetAddress = original.EditStreetAddress;
                EditZipCode = original.EditZipCode;
                EditCity = original.EditCity;
                EditCountry = original.EditCountry;
            }

            //Model => InputModel constructor
            public AddressIM(Models.Interfaces.IAddress original)
            {
                if (original == null)
                {
                    StatusIM = StatusIM.Unknown;
                    return;
                }

                StatusIM = StatusIM.Unchanged;
                AddressId = original.AddressId;
                StreetAddress = EditStreetAddress = original.StreetAddress;
                ZipCode = EditZipCode = original.ZipCode;
                City = EditCity = original.City;
                Country = EditCountry = original.Country;
            }

            //InputModel => Model
            public Address UpdateModel(Address model)
            {
                model.AddressId = AddressId;
                model.StreetAddress = StreetAddress;
                model.ZipCode = ZipCode;
                model.City = City;
                model.Country = Country;
                return model;
            }
        }
    }
}
