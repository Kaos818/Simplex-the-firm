using SimplexLawFirm.Models;
namespace SimplexLawFirm.Services.CurrentUser;
public interface ICurrentClientService { Task<Client?> GetAsync(CancellationToken cancellationToken = default); }
