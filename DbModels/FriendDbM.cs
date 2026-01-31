using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

using Seido.Utilities.SeedGenerator;
using Models;
using Models.Interfaces;
using Models.DTO;

namespace DbModels;

[Table("Friends", Schema = "supusr")]
[Index(nameof(FirstName), nameof(LastName))]
[Index(nameof(LastName), nameof(FirstName))]
sealed public class FriendDbM : csFriend, ISeed<FriendDbM>
{
    [Key]
    public override Guid FriendId { get; set; }

    [Required]
    public override string FirstName { get; set; }

    [JsonIgnore]
    public Guid? AddressId { get; set; }

    #region implementing entity Navigation properties when model is using interfaces in the relationships between models
    [NotMapped]
    public override IAddress Address { get => AddressDbM; set => new NotImplementedException(); }
    [JsonIgnore]
    [ForeignKey("AddressId")]
    public AddressDbM AddressDbM { get; set; } = null;    //This is implemented in the database table

    [NotMapped]
    public override List<IPet> Pets { get => PetsDbM?.ToList<IPet>(); set => new NotImplementedException(); }
    [JsonIgnore]
    public List<PetDbM> PetsDbM { get; set; } = null;

    [NotMapped]
    public override List<IQuote> Quotes { get => QuotesDbM?.ToList<IQuote>(); set => new NotImplementedException(); }
    [JsonIgnore]
    public List<QuoteDbM> QuotesDbM { get; set; } = null;
    #endregion

    #region randomly seed this instance
    public override FriendDbM Seed(SeedGenerator sgen)
    {
        base.Seed(sgen);
        return this;
    }
    #endregion

    #region Update from DTO
    public FriendDbM UpdateFromDTO(FriendCuDto org)
    {
        FirstName = org.FirstName;
        LastName = org.LastName;
        Birthday = org.Birthday;
        Email = org.Email;

        return this;
    }
    #endregion

    #region constructors
    public FriendDbM() { }
    public FriendDbM(FriendCuDto org)
    {
        FriendId = Guid.NewGuid();
        UpdateFromDTO(org);
    }
    #endregion
}

