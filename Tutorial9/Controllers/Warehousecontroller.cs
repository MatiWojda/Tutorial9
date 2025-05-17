using Tutorial9.Models.DTOs;
using Tutorial9.Services;
using Microsoft.AspNetCore.Mvc;

namespace Tutorial9.Controllers
{
    [ApiController]
    [Route("api/warehouse")]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _service;

        public WarehouseController(IWarehouseService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] WarehouseRequest request)
        {
            try
            {
                var id = await _service.AddProductToWarehouseAsync(request);
                return Ok(new { IdProductWarehouse = id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("procedure")]
        public async Task<IActionResult> AddProductViaProcedure([FromBody] WarehouseRequest request)
        {
            try
            {
                var id = await _service.AddProductUsingProcedureAsync(request);
                return Ok(new { IdProductWarehouse = id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}