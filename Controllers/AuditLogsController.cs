using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

/// <summary>Read-only view of the audit trail (admin.view). There is deliberately no create/update/delete.</summary>
[Route("api/audit-logs")]
public class AuditLogsController(ICrudService service) : CrudControllerBase(service)
{
    protected override string Entity => "audit_log";
    public override string PermissionModule => "admin";
}
