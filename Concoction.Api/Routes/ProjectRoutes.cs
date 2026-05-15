using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Api.Routes;

public static class ProjectRoutes
{
    public static RouteGroupBuilder MapProjectRoutes(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/workspaces/{workspaceId:guid}/projects").WithTags("Projects");

        group.MapPost("/", async (
            Guid workspaceId,
            CreateProjectRequest req,
            IProjectService projectService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var project = await projectService.CreateAsync(
                new CreateProjectCommand(workspaceId, req.Name, userId), ct).ConfigureAwait(false);
            return Results.Ok(project);
        }).WithName("CreateProject");

        group.MapGet("/", async (
            Guid workspaceId,
            IProjectService projectService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var projects = await projectService.ListAsync(workspaceId, userId, ct).ConfigureAwait(false);
            return Results.Ok(projects);
        }).WithName("ListProjects");

        group.MapGet("/{projectId:guid}", async (
            Guid workspaceId,
            Guid projectId,
            IProjectService projectService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var project = await projectService.GetByIdAsync(projectId, userId, ct).ConfigureAwait(false);
            return project is null ? Results.NotFound() : Results.Ok(project);
        }).WithName("GetProject");

        group.MapPatch("/{projectId:guid}/name", async (
            Guid workspaceId,
            Guid projectId,
            RenameProjectRequest req,
            IProjectService projectService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var project = await projectService.RenameAsync(projectId, req.Name, userId, ct).ConfigureAwait(false);
            return Results.Ok(project);
        }).WithName("RenameProject");

        group.MapPost("/{projectId:guid}/archive", async (
            Guid workspaceId,
            Guid projectId,
            IProjectService projectService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var project = await projectService.ArchiveAsync(projectId, userId, ct).ConfigureAwait(false);
            return Results.Ok(project);
        }).WithName("ArchiveProject");

        group.MapGet("/{projectId:guid}/databases", async (
            Guid workspaceId,
            Guid projectId,
            IProjectDatabaseCatalog databaseCatalog,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var databases = await databaseCatalog.ListAsync(projectId, userId, ct).ConfigureAwait(false);
            return Results.Ok(databases);
        }).WithName("ListProjectDatabases");

        group.MapPost("/{projectId:guid}/databases", async (
            Guid workspaceId,
            Guid projectId,
            AddProjectDatabaseRequest req,
            IProjectDatabaseCatalog databaseCatalog,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.GetUserId();
            var db = await databaseCatalog.AddAsync(
                new AddDatabaseCommand(projectId, req.Name, req.Type, req.Provider, req.ConnectionRefId, userId), ct)
                .ConfigureAwait(false);
            return Results.Ok(db);
        }).WithName("AddProjectDatabase");

        return group;
    }
}

public sealed record CreateProjectRequest(string Name);
public sealed record RenameProjectRequest(string Name);
public sealed record AddProjectDatabaseRequest(
    string Name,
    ProjectDatabaseType Type,
    string Provider,
    Guid? ConnectionRefId);
