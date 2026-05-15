using Concoction.Application.Abstractions;
using Concoction.Domain.Enums;
using Concoction.Domain.Models;

namespace Concoction.Application.Generation;

public sealed class RunLifecycleService(IRunRepository runRepository, IArtifactStore artifactStore)
{
    public async Task<DatasetRun> StartRunAsync(long seed, IReadOnlyDictionary<string, int> requestedRowCounts, CancellationToken cancellationToken = default)
    {
        var run = new DatasetRun(Guid.NewGuid(), RunStatus.Queued, DateTimeOffset.UtcNow, null, null, seed, null, null, requestedRowCounts);
        return await runRepository.CreateAsync(run, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DatasetRun> MarkRunningAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await GetOrThrowAsync(runId, cancellationToken).ConfigureAwait(false);
        return await runRepository.UpdateAsync(run with { Status = RunStatus.Running, StartedAt = DateTimeOffset.UtcNow }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DatasetRun> CompleteRunAsync(Guid runId, RunManifest manifest, CancellationToken cancellationToken = default)
    {
        var run = await GetOrThrowAsync(runId, cancellationToken).ConfigureAwait(false);
        var completed = run with
        {
            Status = RunStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            ValidationIssueCount = manifest.ValidationIssueCount,
            ArtifactChecksums = manifest.ArtifactChecksums,
            ArtifactPaths = manifest.ArtifactPaths
        };
        return await runRepository.UpdateAsync(completed, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DatasetRun> FailRunAsync(Guid runId, string reason, CancellationToken cancellationToken = default)
    {
        var run = await GetOrThrowAsync(runId, cancellationToken).ConfigureAwait(false);
        var failed = run with
        {
            Status = RunStatus.Failed,
            CompletedAt = DateTimeOffset.UtcNow,
            FailureReason = reason
        };
        return await runRepository.UpdateAsync(failed, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DatasetRun> CancelRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await GetOrThrowAsync(runId, cancellationToken).ConfigureAwait(false);
        if (run.Status is not (RunStatus.Queued or RunStatus.Running))
        {
            throw new InvalidOperationException($"Cannot cancel run in status '{run.Status}'.");
        }

        var cancelled = run with { Status = RunStatus.Cancelled, CompletedAt = DateTimeOffset.UtcNow };
        return await runRepository.UpdateAsync(cancelled, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> StoreArtifactAsync(Guid runId, string name, Stream content, CancellationToken cancellationToken = default)
        => await artifactStore.StoreAsync(runId.ToString(), name, content, cancellationToken).ConfigureAwait(false);

    public Task<DatasetRun?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
        => runRepository.GetByIdAsync(runId, cancellationToken);

    public Task<IReadOnlyList<DatasetRun>> ListRunsAsync(int pageSize = 20, int page = 1, CancellationToken cancellationToken = default)
        => runRepository.ListAsync(pageSize, page, cancellationToken);

    private async Task<DatasetRun> GetOrThrowAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await runRepository.GetByIdAsync(runId, cancellationToken).ConfigureAwait(false);
        return run ?? throw new InvalidOperationException($"Run '{runId}' not found.");
    }
}
