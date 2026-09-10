using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentAccessService
{
    private readonly ApplicationDbContext _context;

    public DocumentAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanAccessAsync(Document document, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return false;
        if (user.Role == UserRole.Administrator || document.UploadedByUserId == userId) return true;
        if (await _context.DocumentShares.AnyAsync(s => s.DocumentId == document.DocumentId && s.UserId == userId && s.IsActive)) return true;
        if (!document.ProjectId.HasValue) return false;
        return user.Role is UserRole.ProjectManager or UserRole.TeamLead
            ? await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)))
            : await _context.ProjectMembers.AnyAsync(m => m.ProjectId == document.ProjectId && m.UserId == userId);
    }

    public async Task<bool> CanManageAsync(Document document, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return false;
        if (user.Role == UserRole.Administrator || document.UploadedByUserId == userId) return true;
        return document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId);
    }

    public async Task<bool> CanUseProjectAsync(int projectId, int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user?.Role == UserRole.Administrator) return true;
        return await _context.Projects.AnyAsync(p => p.ProjectId == projectId &&
            (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)));
    }
}
