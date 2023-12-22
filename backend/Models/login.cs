using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace backend.Models.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.UserId) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest("Invalid request");
            }

            // Connect to your SQL Server database
            string connectionString = "server=103.190.54.22,1633\\SQLEXPRESS;database=hrms_app;user=ecohrms;Password=EcoHrms@123;Encrypt=False";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if the user exists in the database
                string query = "SELECT TOP 1 EmpCode, Password, Salt, Algo, isactive FROM hrms_app.dbo.empCreds WHERE EmpCode = @UserId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", loginRequest.UserId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // User is found, now verify the password
                            var storedPassword = reader["Password"].ToString();
                            var salt = reader["Salt"].ToString();
                            var algorithm = reader["Algo"].ToString();

                            // Use PasswordHasher to verify the entered password against the stored hashed password
                            var passwordHasher = new PasswordHasher<object>();
                            var hashedPassword = HashPassword(loginRequest.Password, salt, algorithm);

                            var result = passwordHasher.VerifyHashedPassword(null, storedPassword, hashedPassword);

                            if (result == PasswordVerificationResult.Success)
                            {
                                // Passwords match, user is authenticated
                                var userData = new UserData
                                {
                                    UserId = reader["EmpCode"].ToString(),
                                    Status = reader["isactive"].ToString()
                                };

                                // Continue with the rest of your code

                                var response = new LoginResponse
                                {
                                    IsSuccessful = true,
                                    Message = "Login successful",
                                    UserData = userData
                                };

                                return Ok(response);
                            }
                        }
                    }
                }
            }

            // Authentication failed
            var failedResponse = new LoginResponse
            {
                IsSuccessful = false,
                Message = "Invalid credentials",
                UserData = null
            };

            return Unauthorized(failedResponse);
        }

        private string HashPassword(string password, string salt, string algorithm)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltBytes = Convert.FromBase64String(salt);

            using (var hasher = new HMACSHA256(saltBytes))
            {
                var hashedBytes = hasher.ComputeHash(passwordBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}