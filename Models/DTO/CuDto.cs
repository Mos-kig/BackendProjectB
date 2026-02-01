using System.Text.RegularExpressions;
using Models.Interfaces;

namespace Models.DTO;

//DTO is a DataTransferObject, can be instanstiated by the controller logic
//and represents a, fully instantiable, subset of the Database models
//for a specific purpose.

//These DTO are simplistic and used to Update and Create objects
public class FriendCuDto
{
    public virtual Guid? FriendId { get; set; }

    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }

    public virtual string Email { get; set; }

    public DateTime? Birthday { get; set; } = null;

    public virtual Guid? AddressId { get; set; } = null;

    public virtual List<Guid> PetsId { get; set; } = null;

    public virtual List<Guid> QuotesId { get; set; } = null;

    public FriendCuDto() { }
    public FriendCuDto(IFriend org)
    {
        FriendId = org.FriendId;
        FirstName = org.FirstName;
        LastName = org.LastName;
        Email = org.Email;
        Birthday = org.Birthday;

        this.AddressId = org?.Address?.AddressId;
        PetsId = org.Pets?.Select(i => i.PetId).ToList();
        QuotesId = org.Quotes?.Select(i => i.QuoteId).ToList();
    }
    public void EnsureValidity()
    {
        // RegEx check to ensure filter only contains a-z, 0-9, and spaces
        if (!string.IsNullOrEmpty(FirstName) && !Regex.IsMatch(FirstName, @"^[a-zA-Z0-9\s]*$"))
        {
            throw new ArgumentException("FirstName can only contain letters (a-z), numbers (0-9), and spaces.");
        }
        if (!string.IsNullOrEmpty(LastName) && !Regex.IsMatch(LastName, @"^[a-zA-Z0-9\s]*$"))
        {
            throw new ArgumentException("LastName can only contain letters (a-z), numbers (0-9), and spaces.");
        }
        if (!string.IsNullOrEmpty(Email) && !Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException("Email has to be a valid email address.");
        }
        if (Birthday.HasValue)
        {
            // Use DateTime.Parse to validate the date by converting back to string and parsing
            var dateString = Birthday.Value.ToString("yyyy-MM-dd");
            var parsedDate = DateTime.Parse(dateString);

            // Additional checks for reasonable birthday range
            if (parsedDate != Birthday.Value || parsedDate.Year < 1900 || parsedDate > DateTime.Now)
            {
                throw new ArgumentException("Birthday must be a valid date in the past (after 1900) or null.");
            }
        }
    }
}

public class AddressCuDto
{
    public virtual Guid? AddressId { get; set; }

    public virtual string StreetAddress { get; set; }
    public virtual int ZipCode { get; set; }
    public virtual string City { get; set; }
    public virtual string Country { get; set; }

    public virtual List<Guid> FriendsId { get; set; } = null;

    public AddressCuDto() { }
    public AddressCuDto(IAddress org)
    {
        AddressId = org.AddressId;
        StreetAddress = org.StreetAddress;
        ZipCode = org.ZipCode;
        City = org.City;
        Country = org.Country;

        FriendsId = org.Friends?.Select(i => i.FriendId).ToList();
    }

    public void EnsureValidity()
    {
        // RegEx check to ensure filter only contains a-z, 0-9, and spaces
        if (!string.IsNullOrEmpty(StreetAddress) && !Regex.IsMatch(StreetAddress, @"^[a-zA-Z0-9\s]*$"))
        {
            throw new ArgumentException("StreetAddress can only contain letters (a-z), numbers (0-9), and spaces.");
        }
        if (!string.IsNullOrEmpty(City) && !Regex.IsMatch(City, @"^[a-zA-Z0-9\s]*$"))
        {
            throw new ArgumentException("City can only contain letters (a-z), numbers (0-9), and spaces.");
        }
        if (!string.IsNullOrEmpty(Country) && !Regex.IsMatch(Country, @"^[a-zA-Z0-9\s]*$"))
        {
            throw new ArgumentException("Country can only contain letters (a-z), numbers (0-9), and spaces.");
        }
        if (ZipCode <= 0) throw new ArgumentException("ZipCode has to be larger than zero");
    }
}

public class PetCuDto
{
    //cannot be nullable as a Pets has to have an owner even when created
    public virtual Guid FriendId { get; set; }

    public virtual Guid? PetId { get; set; }

    public virtual AnimalKind Kind { get; set; }
    public virtual string Name { get; set; }
    public AnimalMood Mood { get; set; }

    public PetCuDto() { }
    public PetCuDto(IPet org)
    {
        FriendId = org.Friend.FriendId;

        PetId = org.PetId;
        Kind = org.Kind;
        Name = org.Name;
        Mood = org.Mood;
    }

    public void EnsureValidity()
    {
        // RegEx check to ensure filter only contains a-z, 0-9, and spaces
        if (!string.IsNullOrEmpty(Name) && !Regex.IsMatch(Name, @"^[a-zA-Z0-9\s]*$"))
        {
            throw new ArgumentException("Name can only contain letters (a-z), numbers (0-9), and spaces.");
        }
        if (!Enum.IsDefined(typeof(AnimalKind), Kind)) throw new ArgumentException("Kind has to be set to a valid value");
        if (!Enum.IsDefined(typeof(AnimalMood), Mood)) throw new ArgumentException("Mood has to be set to a valid value");
    }
}

public class QuoteCuDto
{
    public virtual Guid? QuoteId { get; set; }
    public virtual string Quote { get; set; }
    public virtual string Author { get; set; }

    public virtual List<Guid> FriendsId { get; set; } = null;


    public QuoteCuDto() { }
    public QuoteCuDto(IQuote org)
    {
        QuoteId = org.QuoteId;

        Quote = org.QuoteText;
        Author = org.Author;

        FriendsId = org.Friends?.Select(i => i.FriendId).ToList();
    }


    public void EnsureValidity()
    {
        // RegEx check to ensure filter only contains a-z, 0-9, spaces, and punctuation (.,!?')
        if (!string.IsNullOrEmpty(Quote) && !Regex.IsMatch(Quote, @"^[a-zA-Z0-9\s.,!?']*$"))
        {
            throw new ArgumentException("Quote can only contain letters (a-z), numbers (0-9), spaces, and punctuation (.,!?').");
        }
        if (!string.IsNullOrEmpty(Author) && !Regex.IsMatch(Author, @"^[a-zA-Z0-9\s]*$"))
        {
            throw new ArgumentException("Author can only contain letters (a-z), numbers (0-9), and spaces.");
        }
    }
}

