using Tutorial9.Models.DTOs;
namespace Tutorial9.Services;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouseAsync(WarehouseRequest request);
    Task<int> AddProductUsingProcedureAsync(WarehouseRequest request);
}