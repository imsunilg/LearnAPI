using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[Route("api/brokers")]
public class BrokersController(ICrudService service) : CrudControllerBase<BrokerRequest>(service)
{
    protected override string Entity => "broker";
    public override string PermissionModule => "brokers";
}
