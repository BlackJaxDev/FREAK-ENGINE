using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using XREngine.Data.Server;
using XREngine.Networking.Commands.Database;

namespace XREngine.Networking.Commands
{
    public abstract partial class CommandProcessor
    {
        public static partial class Auth
        {
            /// <summary>
            /// This command lets the user create an authorized session.
            /// </summary>
            /// <param name="command"></param>
            /// <returns></returns>
            public static async Task<(EServerResponseCode code, byte[]? responseData)> Run(string commandData, DatabaseConnection db)
            {
                Data? data = JsonConvert.DeserializeObject<Data>(commandData);

                if (data is null || string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrWhiteSpace(data.Password))
                    return (EServerResponseCode.InvalidTokenJson, null);

                return data.Action switch
                {
                    Data.ECommand.Login => await Login(data.Username, data.Password, db),
                    Data.ECommand.Register => await Register(data.Username, data.Password, db),
                    _ => (EServerResponseCode.InvalidTokenJson, null),
                };
            }

            private static async Task<(EServerResponseCode code, byte[]? responseData)> Login(string username, string password, DatabaseConnection db)
            {
                bool isAuthenticated = await AttemptLogin(username, password, db);
                if (isAuthenticated)
                {
                    var token = await GenerateSessionToken(username, db);
                    var responseData = Encoding.UTF8.GetBytes(token);
                    return (EServerResponseCode.Success, responseData);
                }
                else
                    return (EServerResponseCode.AuthenticationFailed, null);
            }

            private static async Task<(EServerResponseCode code, byte[]? responseData)> Register(string username, string password, DatabaseConnection db)
            {
                bool added = await AttemptRegister(username, password, db);
                return (added ? EServerResponseCode.Success : EServerResponseCode.AlreadyRegistered, null);
            }

            private static async Task<char[]> GenerateSessionToken(string username, DatabaseConnection db)
            {
                var sessionToken = Guid.NewGuid().ToString().ToCharArray();
                await RegisterSession(username, sessionToken, db);
                return sessionToken;
            }

            private static async Task RegisterSession(string username, char[] sessionToken, DatabaseConnection db)
            {
                using SqliteConnection connection = new(db.ConnectionString);
                await connection.OpenAsync();

                string query = "INSERT INTO sessions (username, sessionToken) VALUES (@username, @sessionToken)";
                using SqliteCommand command = new(query, connection);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@sessionToken", new string(sessionToken));

                await command.ExecuteNonQueryAsync();
            }

            public static async Task<bool> AttemptLogin(string username, string password, DatabaseConnection db)
            {
                using SqliteConnection connection = new(db.ConnectionString);
                await connection.OpenAsync();

                string query = $"SELECT password, salt FROM {db.UsersTableName} WHERE username = @username";
                using SqliteCommand command = new(query, connection);
                command.Parameters.AddWithValue("@username", username);

                using SqliteDataReader reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string storedHash = reader.GetString(0);
                    string storedSalt = reader.GetString(1);
                    return VerifyPassword(password, storedHash, storedSalt);
                }
                else
                    return false;
            }

            public static async Task<bool> AttemptRegister(string username, string password, DatabaseConnection db)
            {
                string salt = GenerateSalt();
                string hashedPassword = HashPassword(password + salt);

                using SqliteConnection connection = new(db.ConnectionString);
                await connection.OpenAsync();

                //Check if the user already exists
                string query = $"SELECT COUNT(*) FROM {db.UsersTableName} WHERE username = @username";
                using (SqliteCommand command = new(query, connection))
                {
                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    if (count > 0)
                        return false;
                }

                //Add the user
                query = $"INSERT INTO {db.UsersTableName} (username, password, salt) VALUES (@username, @password, @salt)";
                using (SqliteCommand command = new(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", hashedPassword);
                    command.Parameters.AddWithValue("@salt", salt);

                    await command.ExecuteNonQueryAsync();
                }

                return true;
            }

            private static string GenerateSalt()
            {
                byte[] saltBytes = new byte[16];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }

            private static string HashPassword(string password)
            {
                byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new();
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }

            private static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
                => HashPassword(enteredPassword + storedSalt) == storedHash;

            public class Data
            {
                public enum ECommand
                {
                    Login,
                    Register
                }
                public ECommand Action { get; set; }
                public string? Username { get; set; }
                public string? Password { get; set; }
            }
        }
    }
}
