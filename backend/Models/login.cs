using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;
using System.Data.SqlClient;

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
                string query = "SELECT TOP 1 userid, email, city, password, status FROM ecohrms.userdata WHERE userid = @UserId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", loginRequest.UserId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // User is found, now verify the password
                            var hashedPassword = reader["password"].ToString();

                            // Use PasswordHasher to verify the entered password against the stored hashed password
                            var passwordHasher = new PasswordHasher<object>();
                            var result = passwordHasher.VerifyHashedPassword(null, hashedPassword, loginRequest.Password);

                            if (result == PasswordVerificationResult.Success)
                            {
                                // Passwords match, user is authenticated
                                var userData = new UserData
                                {
                                    UserId = reader["userid"].ToString(),
                                    Email = reader["email"].ToString(),
                                    City = reader["city"].ToString(),
                                    Status = "A"
                                };

                                // Close the data reader before executing the update command
                                reader.Close();

                                // Update the status to 'A' for active state
                                string updateStatusQuery = "UPDATE ecohrms.userdata SET status = @Status WHERE userid = @UserId AND status = 'R'";
                                using (SqlCommand updateCommand = new SqlCommand(updateStatusQuery, connection))
                                {
                                    updateCommand.Parameters.AddWithValue("@UserId", loginRequest.UserId);
                                    updateCommand.Parameters.AddWithValue("@Status", "A");
                                    updateCommand.ExecuteNonQuery();
                                }

                                // Update the status to 'A' for active state in ecohrms.RegistrationKeys table
                                string updateRegistrationKeysQuery = "UPDATE ecohrms.RegistrationKeys SET status = @Status WHERE userid = @UserId AND status = 'R'";
                                using (SqlCommand updateRegistrationKeysCommand = new SqlCommand(updateRegistrationKeysQuery, connection))
                                {
                                    updateRegistrationKeysCommand.Parameters.AddWithValue("@UserId", loginRequest.UserId);
                                    updateRegistrationKeysCommand.Parameters.AddWithValue("@Status", "A");
                                    updateRegistrationKeysCommand.ExecuteNonQuery();
                                }

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
    }

}


public class LoginRequest
    {
        public string? UserId { get; set; }
        public string? Password { get; set; }
        public string? Registrationkey { get; set; }
    }

    public class UserData
    {
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? City { get; set; }
        public string? Status { get; set; }

        public UserData()
        {
            UserId = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCors("AllowAll");

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

