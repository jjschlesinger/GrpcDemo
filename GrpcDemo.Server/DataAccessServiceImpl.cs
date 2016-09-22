using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Npgsql;

namespace GrpcDemo.Server
{
    public class DataAccessServiceImpl : DataAccessService.DataAccessServiceBase
    {
        private const string ConnectionString = "Server=localhost;Port=5432;Database=grpcdemo;User Id=postgres;Password=postgres";

        public override Task<DataResponse> GetDataUnary(DataRequest request, ServerCallContext context)
        {
            var response = new DataResponse();
            response.Record.AddRange(GetRecords());
            return Task.FromResult(response);
        }

        public override async Task GetDataServerStreaming(DataRequest request, IServerStreamWriter<DataReponseRecord> responseStream, ServerCallContext context)
        {
            await GetRecords(responseStream);
        }

        public override async Task<DataResponse> GetDataClientStreaming(IAsyncStreamReader<DataRequestRecord> requestStream, ServerCallContext context)
        {
            var resp = new DataResponse();
            while (await requestStream.MoveNext())
            {
                var req = requestStream.Current;
                var rec = GetRecords(req.Id).SingleOrDefault();
                resp.Record.Add(rec);
            }

            return resp;
        }

        public override async Task GetDataBidirectionalStreaming(IAsyncStreamReader<DataRequestRecord> requestStream, IServerStreamWriter<DataReponseRecord> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var req = requestStream.Current;
                await GetRecords(responseStream, req.Id);

            }
        }

        private static IDbConnection GetConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }

        private static IEnumerable<DataReponseRecord> GetRecords(int id = 0)
        {
            var records = new List<DataReponseRecord>();

            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "select id, message from messages";
                if (id > 0)
                {
                    cmd.CommandText += " where id=@id";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "id";
                    p.DbType = DbType.Int32;
                    p.Value = id;
                    cmd.Parameters.Add(p);
                }
                conn.Open();
                using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (!reader.IsClosed && reader.Read())
                    {
                        records.Add(new DataReponseRecord { Id = Convert.ToInt32(reader["id"]), Message = reader["message"].ToString()});
                    }
                }
            }

            return records;
        }

        private static async Task GetRecords(IServerStreamWriter<DataReponseRecord> responseStream, int id = 0)
        {
            using (var conn = GetConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "select id, message from messages";
                if (id > 0)
                {
                    cmd.CommandText += " where id=@id";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "id";
                    p.DbType = DbType.Int32;
                    p.Value = id;
                    cmd.Parameters.Add(p);
                }
                conn.Open();
                using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    while (!reader.IsClosed && reader.Read())
                    {
                        await responseStream.WriteAsync(new DataReponseRecord {Id = Convert.ToInt32(reader["id"]), Message = reader["message"].ToString()});
                    }
                }
            }
        }
    }
}
