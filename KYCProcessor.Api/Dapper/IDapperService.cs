using System.Data.Common;
using System.Data;
using Dapper;

namespace KYCProcessor.Api.Dapper;

public interface IDapperService
{
    DbConnection GetDbconnection();
    T Get<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure);
    List<T> GetAll<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure);
    int Execute(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure);
    T Insert<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure);
    T InsertTransaction<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure);
    T Update<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.StoredProcedure);
    List<T> GetAllSQL<T>(string sp, DynamicParameters parms, CommandType commandType = CommandType.Text);
}
