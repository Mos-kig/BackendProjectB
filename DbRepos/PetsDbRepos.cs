using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Data;

using Models.Interfaces;
using Models.DTO;
using DbModels;
using DbContext;

namespace DbRepos;

public class PetsDbRepos
{
    private ILogger<PetsDbRepos> _logger;
    private readonly MainDbContext _dbContext;

    public PetsDbRepos(ILogger<PetsDbRepos> logger, MainDbContext context)
    {
        _logger = logger;
        _dbContext = context;
    }

    public async Task<ResponseItemDto<IPet>> ReadPetAsync(Guid id, bool flat)
    {
        IPet item;
        if (!flat)
        {
            //make sure the model is fully populated, try without include.
            //remove tracking for all read operations for performance and to avoid recursion/circular access
            var query = _dbContext.Pets.AsNoTracking()
                .Include(i => i.FriendDbM)
                .ThenInclude(i => i.AddressDbM)
                .Include(i => i.FriendDbM)
                .ThenInclude(i => i.QuotesDbM)
                .Where(i => i.PetId == id);

            item = await query.FirstOrDefaultAsync<IPet>();
        }
        else
        {
            //Not fully populated, compare the SQL Statements generated
            //remove tracking for all read operations for performance and to avoid recursion/circular access
            var query = _dbContext.Pets.AsNoTracking()
                .Where(i => i.PetId == id);

            item = await query.FirstOrDefaultAsync<IPet>();
        }

        if (item == null) throw new ArgumentException($"Item {id} is not existing");
        return new ResponseItemDto<IPet>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            Item = item
        };
    }

    public async Task<ResponsePageDto<IPet>> ReadPetsAsync(bool seeded, bool flat, string filter, int pageNumber, int pageSize)
    {
        filter ??= "";
        IQueryable<PetDbM> query;
        if (flat)
        {
            query = _dbContext.Pets.AsNoTracking();
        }
        else
        {
            query = _dbContext.Pets.AsNoTracking()
                .Include(i => i.FriendDbM)
                .ThenInclude(i => i.AddressDbM)
                .Include(i => i.FriendDbM)
                .ThenInclude(i => i.QuotesDbM);
        }

        var ret = new ResponsePageDto<IPet>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            DbItemsCount = await query

            //Adding filter functionality
            .Where(i => (i.Seeded == seeded) &&
                        (i.Name.ToLower().Contains(filter) ||
                            i.strMood.ToLower().Contains(filter) ||
                            i.strKind.ToLower().Contains(filter) ||
                            (i.FriendDbM.AddressDbM != null && i.FriendDbM.AddressDbM.Country.ToLower().Contains(filter)) ||
                            (i.FriendDbM.AddressDbM != null && i.FriendDbM.AddressDbM.City.ToLower().Contains(filter)))).CountAsync(),

            PageItems = await query

            //Adding filter functionality
            .Where(i => (i.Seeded == seeded) &&
                        (i.Name.ToLower().Contains(filter) ||
                            i.strMood.ToLower().Contains(filter) ||
                            i.strKind.ToLower().Contains(filter) ||
                            (i.FriendDbM.AddressDbM != null && i.FriendDbM.AddressDbM.Country.ToLower().Contains(filter)) ||
                            (i.FriendDbM.AddressDbM != null && i.FriendDbM.AddressDbM.City.ToLower().Contains(filter))))

            //Adding paging
            .Skip(pageNumber * pageSize)
            .Take(pageSize)

            .ToListAsync<IPet>(),

            PageNr = pageNumber,
            PageSize = pageSize
        };
        return ret;
    }

    public async Task<ResponseItemDto<IPet>> DeletePetAsync(Guid id)
    {
        var query1 = _dbContext.Pets
            .Where(i => i.PetId == id);

        var item = await query1.FirstOrDefaultAsync<PetDbM>();

        //If the item does not exists
        if (item == null) throw new ArgumentException($"Item {id} is not existing");

        //delete in the database model
        _dbContext.Pets.Remove(item);

        //write to database in a UoW
        await _dbContext.SaveChangesAsync();
        return new ResponseItemDto<IPet>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            Item = item
        };
    }

    public async Task<ResponseItemDto<IPet>> UpdatePetAsync(PetCuDto itemDto)
    {
        var query1 = _dbContext.Pets
            .Where(i => i.PetId == itemDto.PetId);
        var item = await query1
                .Include(i => i.FriendDbM)
                .FirstOrDefaultAsync<PetDbM>();

        //If the item does not exists
        if (item == null) throw new ArgumentException($"Item {itemDto.PetId} is not existing");

        //transfer any changes from DTO to database objects
        //Update individual properties 
        item.UpdateFromDTO(itemDto);

        //Update navigation properties
        await navProp_PetCUdto_to_PetDbM(itemDto, item);

        //write to database model
        _dbContext.Pets.Update(item);

        //write to database in a UoW
        await _dbContext.SaveChangesAsync();

        //return the updated item in non-flat mode
        return await ReadPetAsync(item.PetId, false);
    }

    public async Task<ResponseItemDto<IPet>> CreatePetAsync(PetCuDto itemDto)
    {
        if (itemDto.PetId != null)
            throw new ArgumentException($"{nameof(itemDto.PetId)} must be null when creating a new object");

        //transfer any changes from DTO to database objects
        //Update individual properties
        var item = new PetDbM(itemDto);

        //Update navigation properties
        await navProp_PetCUdto_to_PetDbM(itemDto, item);

        //write to database model
        _dbContext.Pets.Add(item);

        //write to database in a UoW
        await _dbContext.SaveChangesAsync();

        //return the updated item in non-flat mode
        return await ReadPetAsync(item.PetId, false);
    }

    private async Task navProp_PetCUdto_to_PetDbM(PetCuDto itemDtoSrc, PetDbM itemDst)
    {
        //update owner, i.e. navigation property FriendDbM
        var owner = await _dbContext.Friends.FirstOrDefaultAsync(
            a => (a.FriendId == itemDtoSrc.FriendId));

        if (owner == null)
            throw new ArgumentException($"Item id {itemDtoSrc.FriendId} not existing");

        itemDst.FriendDbM = owner;
    }
}
