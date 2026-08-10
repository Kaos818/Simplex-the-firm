using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services.CurrentUser;
public sealed class CurrentClientService(IHttpContextAccessor accessor, ApplicationDbContext db) : ICurrentClientService
{
    public async Task<Client?> GetAsync(CancellationToken cancellationToken = default)
    {
        var id = accessor.HttpContext?.Session.GetInt32("UserId");
        if (id is null) return null;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user?.Role != UserRole.Client) return null;
        var email = user.Email.Trim().ToUpper();
        return await db.Clients.SingleOrDefaultAsync(x => x.Email.ToUpper() == email, cancellationToken);
    }
}
