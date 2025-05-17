using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Models.DTOs;

namespace Tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IConfiguration _configuration;

    public WarehouseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductToWarehouseAsync(WarehouseRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0.");

        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")); 
        await using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = (SqlTransaction)transaction;

        try
        {
            command.CommandText = "SELECT COUNT(1) FROM Product WHERE IdProduct = @p";
            command.Parameters.AddWithValue("@p", request.IdProduct);

            var exists = (int)await command.ExecuteScalarAsync();
            if (exists == 0)
                throw new KeyNotFoundException($"Product {request.IdProduct} not found.");
            
            command.Parameters.Clear();
            command.CommandText = "SELECT COUNT(1) FROM Warehouse WHERE IdWarehouse = @w";
            command.Parameters.AddWithValue("@w", request.IdWarehouse);
            exists = (int)await command.ExecuteScalarAsync();
            if (exists == 0)
                throw new KeyNotFoundException($"Warehouse {request.IdWarehouse} not found.");
            
            command.Parameters.Clear();
            command.CommandText = @"SELECT TOP 1 IdOrder
                FROM [Order]
                WHERE IdProduct = @p AND Amount = @a AND CreatedAt < @c
                ORDER BY CreatedAt DESC";
            
            command.Parameters.AddWithValue("@p", request.IdProduct);
            command.Parameters.AddWithValue("@a", request.Amount);
            command.Parameters.AddWithValue("@c", request.CreatedAt);
            var orderObj = await command.ExecuteScalarAsync();
            if (orderObj == null)

                throw new InvalidOperationException("No matching order found.");

            var idOrder = (int)orderObj;
            
            command.Parameters.Clear();
            command.CommandText = "SELECT COUNT(1) FROM Product_Warehouse WHERE IdOrder = @o";
            command.Parameters.AddWithValue("@o", idOrder);
            exists = (int)await command.ExecuteScalarAsync();

            if (exists > 0)
                throw new InvalidOperationException($"Order {idOrder} already fulfilled.");
            
            command.Parameters.Clear();

            command.CommandText = "UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @o";
            command.Parameters.AddWithValue("@o", idOrder);
            await command.ExecuteNonQueryAsync();
            
            command.Parameters.Clear();
            command.CommandText = @"DECLARE @total DECIMAL(18,2) =
                (SELECT Price * @a FROM Product WHERE IdProduct = @p);
                INSERT INTO Product_Warehouse
                (IdProduct, IdWarehouse, Amount, Price, CreatedAt, IdOrder)
                VALUES (@p, @w, @a, @total, GETDATE(), @o);
                SELECT SCOPE_IDENTITY();";
            
            command.Parameters.AddWithValue("@p", request.IdProduct);
            command.Parameters.AddWithValue("@w", request.IdWarehouse);
            command.Parameters.AddWithValue("@a", request.Amount);
            command.Parameters.AddWithValue("@o", idOrder);
            var newIdObj = await command.ExecuteScalarAsync();
            var newId = Convert.ToInt32(newIdObj);


            await transaction.CommitAsync();
            return newId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<int> AddProductUsingProcedureAsync(WarehouseRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0.");

        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand(); 
        command.Connection = connection;
        await connection.OpenAsync();

        command.CommandText = "dbo.AddProductToWarehouse";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        var result = await command.ExecuteScalarAsync();
        if (result == null)
            throw new InvalidOperationException("Procedure did not return an ID.");

        return Convert.ToInt32(result);
    }
}