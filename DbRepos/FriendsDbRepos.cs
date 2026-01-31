using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Data;

using Models.Interfaces;
using Models.DTO;
using DbModels;
using DbContext;

namespace DbRepos;

public class FriendsDbRepos
{
    private ILogger<FriendsDbRepos> _logger;
    private readonly MainDbContext _dbContext;

    public FriendsDbRepos(ILogger<FriendsDbRepos> logger, MainDbContext context)
    {
        _logger = logger;
        _dbContext = context;
    }

    public async Task<ResponseItemDto<IFriend>> ReadFriendAsync(Guid id, bool flat)
    {
        IFriend item;
        if (!flat)
        {
            //make sure the model is fully populated, try without include.
            //remove tracking for all read operations for performance and to avoid recursion/circular access
            var query = _dbContext.Friends.AsNoTracking()
                .Include(i => i.AddressDbM)
                .Include(i => i.PetsDbM)
                .Include(i => i.QuotesDbM)
                .Where(i => i.FriendId == id);

            item = await query.FirstOrDefaultAsync<IFriend>();
        }
        else
        {
            //Not fully populated, compare the SQL Statements generated
            //remove tracking for all read operations for performance and to avoid recursion/circular access
            var query = _dbContext.Friends.AsNoTracking()
                .Where(i => i.FriendId == id);

            item = await query.FirstOrDefaultAsync<IFriend>();
        }

        if (item == null) throw new ArgumentException($"Item {id} is not existing");
        return new ResponseItemDto<IFriend>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            Item = item
        };
    }

    public async Task<ResponsePageDto<IFriend>> ReadFriendsAsync(bool seeded, bool flat, string filter, int pageNumber, int pageSize)
    {
        filter ??= "";
        IQueryable<FriendDbM> query;
        if (flat)
        {
            query = _dbContext.Friends.AsNoTracking();
        }
        else
        {
            query = _dbContext.Friends.AsNoTracking()
                .Include(i => i.AddressDbM)
                .Include(i => i.PetsDbM)
                .Include(i => i.QuotesDbM);
        }

        var ret = new ResponsePageDto<IFriend>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            DbItemsCount = await query

            //filter functionality
            .Where(i => (i.Seeded == seeded) &&
                        (i.FirstName.ToLower().Contains(filter) ||
                            i.LastName.ToLower().Contains(filter) ||
                            (i.AddressDbM != null && i.AddressDbM.Country.ToLower().Contains(filter)) ||
                            (i.AddressDbM != null && i.AddressDbM.City.ToLower().Contains(filter)))).CountAsync(),

            PageItems = await query

            //filter functionality
            .Where(i => (i.Seeded == seeded) &&
                        (i.FirstName.ToLower().Contains(filter) ||
                            i.LastName.ToLower().Contains(filter) ||
                            (i.AddressDbM != null && i.AddressDbM.Country.ToLower().Contains(filter)) ||
                            (i.AddressDbM != null && i.AddressDbM.City.ToLower().Contains(filter))))

            .Skip(pageNumber * pageSize)
            .Take(pageSize)

            .ToListAsync<IFriend>(),

            PageNr = pageNumber,
            PageSize = pageSize
        };
        return ret;
    }

    public async Task<ResponseItemDto<IFriend>> DeleteFriendAsync(Guid id)
    {
        //Find the instance with matching id
        var query1 = _dbContext.Friends
            .Where(i => i.FriendId == id);
        var item = await query1.FirstOrDefaultAsync<FriendDbM>();

        //If the item does not exists
        if (item == null) throw new ArgumentException($"Item {id} is not existing");

        //delete in the database model
        _dbContext.Friends.Remove(item);

        //write to database in a UoW
        await _dbContext.SaveChangesAsync();
        return new ResponseItemDto<IFriend>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            Item = item
        };
    }

    public async Task<ResponseItemDto<IFriend>> UpdateFriendAsync(FriendCuDto itemDto)
    {
        //Find the instance with matching id and read the navigation properties.
        var query1 = _dbContext.Friends
            .Where(i => i.FriendId == itemDto.FriendId);
        var item = await query1
            .Include(i => i.AddressDbM)
            .Include(i => i.PetsDbM)
            .Include(i => i.QuotesDbM)
            .FirstOrDefaultAsync<FriendDbM>();

        //If the item does not exists
        if (item == null) throw new ArgumentException($"Item {itemDto.FriendId} is not existing");

        //transfer any changes from DTO to database objects
        //Update individual properties
        item.UpdateFromDTO(itemDto);

        //Update navigation properties
        await navProp_FriendCUdto_to_FriendDbM(itemDto, item);

        //write to database model
        _dbContext.Friends.Update(item);

        //write to database in a UoW
        await _dbContext.SaveChangesAsync();

        //return the updated item in non-flat mode
        return await ReadFriendAsync(item.FriendId, false);
    }

    public async Task<ResponseItemDto<IFriend>> CreateFriendAsync(FriendCuDto itemDto)
    {
        if (itemDto.FriendId != null)
            throw new ArgumentException($"{nameof(itemDto.FriendId)} must be null when creating a new object");

        //transfer any changes from DTO to database objects
        //Update individual properties Friend
        var item = new FriendDbM(itemDto);

        //Update navigation properties
        await navProp_FriendCUdto_to_FriendDbM(itemDto, item);

        //write to database model
        _dbContext.Friends.Add(item);

        //write to database in a UoW
        await _dbContext.SaveChangesAsync();

        //return the updated item in non-flat mode
        return await ReadFriendAsync(item.FriendId, false);
    }

    //from all Guid relationships in _itemDtoSrc finds the corresponding object in the database and assigns it to _itemDst 
    //as navigation properties. Error is thrown if no object is found corresponing to an id.
    private async Task navProp_FriendCUdto_to_FriendDbM(FriendCuDto itemDtoSrc, FriendDbM itemDst)
    {
        //update AddressDbM from itemDto.AddressId
        itemDst.AddressDbM = (itemDtoSrc.AddressId != null) ? await _dbContext.Addresses.FirstOrDefaultAsync(
            a => (a.AddressId == itemDtoSrc.AddressId)) : null;

        //update PetsDbM from itemDto.PetsId list
        List<PetDbM> pets = null;
        if (itemDtoSrc.PetsId != null)
        {
            pets = new List<PetDbM>();
            foreach (var id in itemDtoSrc.PetsId)
            {
                var p = await _dbContext.Pets.FirstOrDefaultAsync(i => i.PetId == id);
                if (p == null)
                    throw new ArgumentException($"Item id {id} not existing");

                pets.Add(p);
            }
        }
        itemDst.PetsDbM = pets;

        //update QuotesDbM from itemDto.QuotesId
        List<QuoteDbM> quotes = null;
        if (itemDtoSrc.QuotesId != null)
        {
            quotes = new List<QuoteDbM>();
            foreach (var id in itemDtoSrc.QuotesId)
            {
                var q = await _dbContext.Quotes.FirstOrDefaultAsync(i => i.QuoteId == id);
                if (q == null)
                    throw new ArgumentException($"Item id {id} not existing");

                quotes.Add(q);
            }
        }
        itemDst.QuotesDbM = quotes;
    }
}
