using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[Route("api/products")]
public class ProductsController(ICrudService service) : CrudControllerBase<ProductRequest>(service)
{
    protected override string Entity => "product"; // → fn_product_get_all, fn_product_create ...
    public override string PermissionModule => "products";
}

[Route("api/product-types")]
public class ProductTypesController(ICrudService service) : CrudControllerBase<ProductTypeRequest>(service)
{
    protected override string Entity => "product_type";
    public override string PermissionModule => "products";
}
