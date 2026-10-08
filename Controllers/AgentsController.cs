using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[Route("api/agents")]
public class AgentsController(ICrudService service) : CrudControllerBase<AgentRequest>(service)
{
    protected override string Entity => "agent";
    public override string PermissionModule => "agents";
}
