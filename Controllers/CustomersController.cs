using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[Route("api/customers")]
public class CustomersController(ICrudService service) : CrudControllerBase<CustomerRequest>(service)
{
    protected override string Entity => "customer";
    public override string PermissionModule => "customers";
}
