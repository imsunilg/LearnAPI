using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

// Day 20: read-only for now (the user form needs the role list). Day 21 adds role management.
[Route("api/roles")]
public class RolesController(ICrudService service) : CrudControllerBase(service)
{
    protected override string Entity => "role";
    public override string PermissionModule => "roles";
}
