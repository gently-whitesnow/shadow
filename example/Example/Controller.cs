using Example.BO;
using Example.Models;
using Microsoft.AspNetCore.Mvc;

namespace Example.Controllers;

[ApiController]
[Route("v1/entity")]
public class ExampleController(ExampleService exampleService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SaveAsync([FromBody] EntityDto entityDto)
    {
        var (result, entityDbModel) = await exampleService.SaveEntity(entityDto);
        switch (result)
        {
            case Result.Success:
                return Ok(entityDbModel);
            case Result.BadName:
                return BadRequest("Bad name");
        }

        return BadRequest("Unknown error");
    }
}