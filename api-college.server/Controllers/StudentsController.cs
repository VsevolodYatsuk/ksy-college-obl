using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using api_college.server.Models;

namespace api_college.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly string _connectionString = "Host=95.165.153.126;Port=5432;Username=test_user;Password=test_pass;Database=college_ksy";
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(ILogger<StudentsController> logger)
        {
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Login attempt for user: {Login}", request.Login);

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sqlStudent = @"
                        SELECT 
                            s.login_students, 
                            s.password_students_hash, 
                            s.surname_students, 
                            s.name_students, 
                            s.middle_name_students,
                            g.name_groups,
                            g.id_groups,
                            sp.name_specialization,
                            i.name_item
                        FROM students s 
                        JOIN groups g ON s.groups_id = g.id_groups
                        JOIN specialization sp ON g.specialization_id = sp.id_specialization
                        JOIN items i ON sp.items_id = i.id_items
                        WHERE s.login_students = @login";

                    var sqlTeacher = @"
                        SELECT 
                            t.login_teachers, 
                            t.password_teachers_hash,
                            t.surname_teachers,
                            t.name_teachers,
                            t.middle_name_teachers,
                            i.name_item,
                            t.id_teachers
                        FROM teachers t
                        JOIN items i ON t.items_id = i.id_items
                        WHERE t.login_teachers = @login";

                    using (var cmd = new NpgsqlCommand(sqlStudent, connection))
                    {
                        cmd.Parameters.AddWithValue("login", request.Login);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var storedHash = reader.GetString(1);
                                var enteredHash = ComputeSha256Hash(request.Password);
                                if (storedHash == enteredHash)
                                {
                                    _logger.LogInformation("Student {Login} successfully authenticated.", request.Login);

                                    var user = new
                                    {
                                        Login = reader.GetString(0),
                                        Surname = reader.GetString(2),
                                        Name = reader.GetString(3),
                                        MiddleName = reader.GetString(4),
                                        GroupName = reader.GetString(5),
                                        GroupId = reader.GetInt32(6),
                                        SpecializationName = reader.GetString(7),
                                        ItemName = reader.GetString(8),
                                        Role = "student"
                                    };
                                    return Ok(new { message = "Успешный вход", user });
                                }
                                else
                                {
                                    _logger.LogWarning("Password hash mismatch for student {Login}", request.Login);
                                    return Unauthorized("Неверный логин или пароль");
                                }
                            }
                        }
                    }

                    using (var cmd = new NpgsqlCommand(sqlTeacher, connection))
                    {
                        cmd.Parameters.AddWithValue("login", request.Login);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var storedHash = reader.GetString(1);
                                var enteredHash = ComputeSha256Hash(request.Password);
                                if (storedHash == enteredHash)
                                {
                                    _logger.LogInformation("Teacher {Login} successfully authenticated.", request.Login);

                                    var user = new
                                    {
                                        Login = reader.GetString(0),
                                        Surname = reader.GetString(2),
                                        Name = reader.GetString(3),
                                        MiddleName = reader.GetString(4),
                                        ItemName = reader.GetString(5),
                                        TeacherId = reader.GetInt32(6),
                                        Role = "teacher"
                                    };
                                    return Ok(new { message = "Успешный вход", user });
                                }
                                else
                                {
                                    _logger.LogWarning("Password hash mismatch for teacher {Login}", request.Login);
                                    return Unauthorized("Неверный логин или пароль");
                                }
                            }
                        }
                    }

                    _logger.LogWarning("User {Login} not found.", request.Login);
                    return Unauthorized("Неверный логин или пароль");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing login for user {Login}", request.Login);
                return StatusCode(500, $"Ошибка при обработке запроса: {ex.Message}");
            }
        }

        [HttpGet("schedule")]
        public async Task<ActionResult> GetSchedule(int groupId)
        {
            _logger.LogInformation("Fetching schedule for group ID: {GroupId}", groupId);

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            s.id_schedule,
                            s.date_time_item,
                            i.name_item,
                            t.surname_teachers || ' ' || t.name_teachers || ' ' || t.middle_name_teachers as teacherName
                        FROM schedule s
                        JOIN items i ON s.items_id = i.id_items
                        JOIN teachers t ON s.teachers_id = t.id_teachers
                        WHERE s.groups_id = @groupId";

                    var scheduleList = new List<ScheduleItem>();

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("groupId", groupId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var scheduleItem = new ScheduleItem
                                {
                                    IdSchedule = reader.GetInt32(0),
                                    DateTimeItem = reader.GetDateTime(1),
                                    ItemName = reader.GetString(2),
                                    TeacherName = reader.GetString(3)
                                };

                                scheduleList.Add(scheduleItem);
                            }
                        }
                    }

                    return Ok(scheduleList);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching schedule for group ID: {GroupId}", groupId);
                return StatusCode(500, $"Ошибка при обработке запроса: {ex.Message}");
            }
        }

        [HttpGet("teacher-schedule")]
        public async Task<ActionResult> GetTeacherSchedule(int teacherId)
        {
            _logger.LogInformation("Fetching schedule for teacher ID: {TeacherId}", teacherId);

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            s.id_schedule,
                            s.date_time_item,
                            i.name_item,
                            g.name_groups as groupName
                        FROM schedule s
                        JOIN items i ON s.items_id = i.id_items
                        JOIN groups g ON s.groups_id = g.id_groups
                        WHERE s.teachers_id = @teacherId";

                    var scheduleList = new List<ScheduleItem>();

                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("teacherId", teacherId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var scheduleItem = new ScheduleItem
                                {
                                    IdSchedule = reader.GetInt32(0),
                                    DateTimeItem = reader.GetDateTime(1),
                                    ItemName = reader.GetString(2),
                                    GroupName = reader.GetString(3)
                                };

                                scheduleList.Add(scheduleItem);
                            }
                        }
                    }

                    return Ok(scheduleList);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching schedule for teacher ID: {TeacherId}", teacherId);
                return StatusCode(500, $"Ошибка при обработке запроса: {ex.Message}");
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public class LoginRequest
        {
            public string? Login { get; set; }
            public string? Password { get; set; }
        }

        public class ScheduleItem
        {
            public int IdSchedule { get; set; }
            public DateTime DateTimeItem { get; set; }
            public string? ItemName { get; set; }
            public string? TeacherName { get; set; }
            public string? GroupName { get; set; }
        }
    }
}
