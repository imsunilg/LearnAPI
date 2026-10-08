using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

// Day 26: read endpoints (list/get). Day 29 adds create/update/submit/decide.
[Route("api/proposals")]
public class ProposalsController(ICrudService service) : CrudControllerBase(service)
{
    protected override string Entity => "proposal";
    public override string PermissionModule => "proposals";
}
