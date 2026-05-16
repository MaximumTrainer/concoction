using Concoction.Application.Abstractions;
using Concoction.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Concoction.Infrastructure.Persistence;

public sealed class EfAuditLogRepository(ConcoctionDbContext db) : IAuditLogRepository
{
    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        db.AuditEvents.Add(auditEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> QueryAsync(Guid accountId, int skip, int take, string? actionFilter, CancellationToken cancellationToken = default)
    {
        var query = db.AuditEvents.Where(e => e.AccountId == accountId);
        if (actionFilter is not null)
            query = query.Where(e => e.Action.Contains(actionFilter));
        return await query.OrderByDescending(e => e.OccurredAt).Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Guid accountId, string? actionFilter, CancellationToken cancellationToken = default)
    {
        var query = db.AuditEvents.Where(e => e.AccountId == accountId);
        if (actionFilter is not null)
            query = query.Where(e => e.Action.Contains(actionFilter));
        return await query.CountAsync(cancellationToken);
    }
}
