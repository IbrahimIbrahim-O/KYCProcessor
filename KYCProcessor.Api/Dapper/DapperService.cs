using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace KYCProcessor.Api.Dapper;

public class DapperService : IDapperService
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    public DapperService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("DefaultConnection");
    }
    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
    public void Dispose()
    {

    }
    public int Execute(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
    {
        throw new NotImplementedException();
    }

    public T Get<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.Text)
    {
        IDbConnection db = new SqlConnection(_connectionString);
        return db.Query<T>(sp, parms, commandType: commandType, commandTimeout: 120000).FirstOrDefault();
    }

    public List<T> GetAll<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
    {
        IDbConnection db = new SqlConnection(_connectionString);
        return db.Query<T>(sp, parms, commandType: commandType, commandTimeout: 120000).ToList();
    }

    public List<T> GetAllSQL<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.Text)
    {
        IDbConnection db = new SqlConnection(_connectionString);
        return db.Query<T>(sp, parms, commandType: commandType, commandTimeout: 120000).ToList();
    }

    public DbConnection GetDbconnection()
    {
        return new SqlConnection(_connectionString);
    }

    public T Insert<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
    {
        T result;
        IDbConnection db = new SqlConnection(_connectionString);
        try
        {
            if (db.State == ConnectionState.Closed)
                db.Open();

            result = db.Query<T>(sp, parms, commandType: commandType).FirstOrDefault();
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            if (db.State == ConnectionState.Open)
                db.Close();
        }

        return result;
    }

    public T InsertTransaction<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
    {
        T result;
        IDbConnection db = new SqlConnection(_connectionString);
        try
        {
            if (db.State == ConnectionState.Closed)
                db.Open();

            var tran = db.BeginTransaction();
            try
            {
                result = db.Query<T>(sp, parms, commandType: commandType, transaction: tran).FirstOrDefault();
                tran.Commit();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                throw ex;
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            if (db.State == ConnectionState.Open)
                db.Close();
        }

        return result;
    }


    public T Update<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure)
    {
        T result;
        IDbConnection db = new SqlConnection(_connectionString);
        try
        {
            if (db.State == ConnectionState.Closed)
                db.Open();

            var tran = db.BeginTransaction();
            try
            {
                result = db.Query<T>(sp, parms, commandType: commandType, transaction: tran).FirstOrDefault();
                tran.Commit();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                throw ex;
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            if (db.State == ConnectionState.Open)
                db.Close();
        }

        return result;
    }

}
