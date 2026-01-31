using Microsoft.Extensions.Logging;

using Models.DTO;
using DbRepos;
using Services.Interfaces;

namespace Services;

public class AdminServiceDb : IAdminService
{
    private readonly AdminDbRepos _repo = null;
    private readonly ILogger<AdminServiceDb> _logger = null;


    #region constructors
    public AdminServiceDb(AdminDbRepos repo)
    {
        _repo = repo;
    }
    public AdminServiceDb(AdminDbRepos repo, ILogger<AdminServiceDb> logger) : this(repo)
    {
        _logger = logger;
    }
    #endregion

    //Simple 1:1 calls in this case, but as Services expands, this will no longer need to be the case
    public Task<ResponseItemDto<GstUsrInfoAllDto>> GuestInfoAsync() => _repo.InfoAsync();
    public Task<ResponseItemDto<GstUsrInfoAllDto>> SeedAsync(int nrOfItems) => _repo.SeedAsync(nrOfItems);
    public Task<ResponseItemDto<GstUsrInfoAllDto>> RemoveSeedAsync(bool seeded) => _repo.RemoveSeedAsync(seeded);
}

