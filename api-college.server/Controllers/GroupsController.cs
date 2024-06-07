using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using api_college.server.Models;

namespace api_college.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly string _connectionString = "Host=95.165.153.126;Port=5432;Username=test_user;Password=test_pass;Database=college_ksy";

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Group>>> Get()
        {
            try
            {
                List<Group> groups = new List<Group>();

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var cmd = new NpgsqlCommand("SET CLIENT_ENCODING TO 'UTF8'; SELECT * FROM groups", connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var group = new Group
                            {
                                Id = reader.GetInt32(0),
                                SpecializationId = reader.GetInt32(1),
                                Name = reader.GetString(2),
                                NumberOfStudents = reader.GetInt32(3)
                            };
                            groups.Add(group);
                        }
                    }
                }

                return groups;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении данных из базы данных: {ex.Message}");
            }
        }
    }
}
