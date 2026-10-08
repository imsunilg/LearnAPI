using LearnAPI.Application.DTOs;
using LearnAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearnAPI.Controllers;

[Route("api/products")]
public class ProductsController(ICrudService service, IJsonDb db) : CrudControllerBase<ProductRequest>(service)
{
    protected override string Entity => "product"; // → fn_product_get_all, fn_product_create ...
    public override string PermissionModule => "products";

    /// <summary>Form definition (fields, sections, rules) of a product line - rendered by the React dynamic form engine.</summary>
    [HttpGet("form-fields/{typeCode}")]
    public async Task<IActionResult> FormFields(string typeCode, CancellationToken ct) =>
        JsonResult(await db.CallAsync("fn_product_get_fields", ct, typeCode));
}

[Route("api/product-types")]
public class ProductTypesController(ICrudService service) : CrudControllerBase<ProductTypeRequest>(service)
{
    protected override string Entity => "product_type";
    public override string PermissionModule => "products";
}
