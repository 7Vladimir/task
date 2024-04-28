using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Task.Integration.Data.Models;
using Task.Integration.Data.Models.Models;

namespace Task.Connector
{
    public class ConnectorDb : IConnector
    {
        public ConnectorDb()
        {
        }

        public void StartUp(string connectionString)
        {
            NpgsqlConnection connection = null;

            string pattern = @"ConnectionString='([^']*)'";
            Match match = Regex.Match(connectionString, pattern);

            string parseConnectionString = null;

            if (match.Success)
            {
                parseConnectionString = match.Groups[1].Value;
            }

            try
            {
                using (connection = new NpgsqlConnection(parseConnectionString))
                {
                    connection.Open();
                }
            }
            catch (NpgsqlException err)
            {
                Logger.Error("Произошла ошибка: " + err.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        public void CreateUser(UserToCreate user)
        {
            NpgsqlConnection connection = null;
            try
            {
                string query =
                    "INSERT INTO \"TestTaskSchema\".\"User\" (login ,\"firstName\" , \"lastName\", \"middleName\", \"telephoneNumber\", \"isLead\") " +
                    "VALUES (@login , @firstName,@lastName , @middleName, @telephoneNumber, @isLead);";

                using (connection =
                           new NpgsqlConnection(
                               "Server=127.0.0.1;Port=5438;Database=testDb;Username=postgres;Password=12345678;"))
                {
                    connection.Open();

                    List<UserProperty> properties = user.Properties.ToList();
                    UserProperty defaultProperty = properties[0];


                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", user.Login);
                        command.Parameters.AddWithValue("@firstName", "testFirstName");
                        command.Parameters.AddWithValue("@lastName", "testLastName");
                        command.Parameters.AddWithValue("@middleName", "testMiddleName");
                        command.Parameters.AddWithValue("@telephoneNumber", "testTelephoneNumer");
                        command.Parameters.AddWithValue("@isLead", bool.Parse(defaultProperty.Value));
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (NpgsqlException err)
            {
                Logger.Error("Произошла ошибка: " + err.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        public IEnumerable<Property> GetAllProperties()
        {
            string query =
                "SELECT u.*, p.* FROM \"TestTaskSchema\".\"User\" u JOIN \"TestTaskSchema\".\"Passwords\" p ON u.login = \"userId\" Limit 1";

            NpgsqlConnection connection = null;
            List<Property> properties = new List<Property>();

            try
            {
                using (connection =
                           new NpgsqlConnection(
                               "Server=127.0.0.1;Port=5438;Database=testDb;Username=postgres;Password=12345678;"))
                {
                    connection.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        NpgsqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            Dictionary<string, string> userProperties = new Dictionary<string, string>()
                            {
                                ["lastName"] = reader["lastName"].ToString(),
                                ["firstName"] = reader["firstName"].ToString(),
                                ["middleName"] = reader["middleName"].ToString(),
                                ["telephoneNumber"] = reader["telephoneNumber"].ToString(),
                                ["isLead"] = reader["isLead"].ToString(),
                                ["password"] = reader["password"].ToString(),
                            };

                            foreach (var kvp in userProperties)
                            {
                                properties.Add(new Property(kvp.Key, kvp.Value));
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException err)
            {
                Logger.Error("Произошла ошибка: " + err.Message);
            }
            finally
            {
                connection.Close();
            }

            return properties;
        }

        public IEnumerable<UserProperty> GetUserProperties(string userLogin)
        {
            string query = "SELECT * FROM \"TestTaskSchema\".\"User\" WHERE login = @login;";

            NpgsqlConnection connection = null;
            List<UserProperty> properties = new List<UserProperty>();

            try
            {
                using (connection =
                           new NpgsqlConnection(
                               "Server=127.0.0.1;Port=5438;Database=testDb;Username=postgres;Password=12345678;"))
                {
                    connection.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", userLogin);

                        NpgsqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            Dictionary<string, string> userProperties = new Dictionary<string, string>()
                            {
                                ["lastName"] = reader["lastName"].ToString(),
                                ["firstName"] = reader["firstName"].ToString(),
                                ["middleName"] = reader["middleName"].ToString(),
                                ["telephoneNumber"] = reader["telephoneNumber"].ToString(),
                                ["isLead"] = reader["isLead"].ToString()
                            };

                            foreach (var kvp in userProperties)
                            {
                                properties.Add(new UserProperty(kvp.Key, kvp.Value));
                            }
                        }
                    }
                }
            }
            catch (NpgsqlException err)
            {
                Logger.Error("Произошла ошибка: " + err.Message);
            }
            finally
            {
                connection.Close();
            }

            return properties;
        }

        public bool IsUserExists(string userLogin)
        {
            string query = "SELECT EXISTS(SELECT 1 FROM \"TestTaskSchema\".\"User\" WHERE login = @login);";

            bool userExist = false;

            using (NpgsqlConnection connection =
                   new NpgsqlConnection(
                       "Server=127.0.0.1;Port=5438;Database=testDb;Username=postgres;Password=12345678;"))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@login", userLogin);
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count > 0)
                    {
                        userExist = true;
                    }
                }
            }

            return userExist;
        }

        public void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin)
        {
            string query = "UPDATE \"TestTaskSchema\".\"User\" SET ";
            NpgsqlConnection connection = null;

            try
            {
                using (connection =
                           new NpgsqlConnection(
                               "Server=127.0.0.1;Port=5438;Database=testDb;Username=postgres;Password=12345678;"))
                {
                    connection.Open();

                    foreach (var property in properties)
                    {
                        query += $"\"{property.Name}\" = @{property.Name}";
                    }

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        foreach (var property in properties)
                        {
                            command.Parameters.AddWithValue($"@{property.Name}", property.Value);
                        }

                        command.Parameters.AddWithValue("@login", userLogin);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (NpgsqlException err)
            {
                Logger.Error("Произошла ошибка: " + err.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        public IEnumerable<Permission> GetAllPermissions()
        {
            string query = "SELECT id, \"name\", \"corporatePhoneNumber\" FROM \"TestTaskSchema\".\"ItRole\"" +
                           "UNION ALL SELECT id ,\"name\" , NULL AS \"corporatePhoneNumber\" FROM \"TestTaskSchema\".\"RequestRight\"";

            NpgsqlConnection connection = null;
            List<Permission> permissions = new List<Permission>();

            try
            {
                using (connection =
                           new NpgsqlConnection(
                               "Server=127.0.0.1;Port=5438;Database=testDb;Username=postgres;Password=12345678;"))
                {
                    connection.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        NpgsqlDataReader reader = command.ExecuteReader();


                        while (reader.Read())
                        {
                            string id = reader["id"].ToString();
                            string name = reader["name"].ToString();
                            string? corporatePhoneNumber = reader["corporatePhoneNumber"].ToString();

                            var permission = new Permission(id, name, corporatePhoneNumber);
                            permissions.Add(permission);
                        }
                    }
                }
            }
            catch (NpgsqlException err)
            {
                Logger.Error("Произошла ошибка: " + err.Message);
            }
            finally
            {
                connection.Close();
            }

            return permissions;
        }

        public void AddUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            string query = "INSERT INTO \"TestTaskSchema\".\"UserITRole\" (\"userId\", \"roleId\") VALUES (@userId, @roleId)";
            NpgsqlConnection connection = null;

            try
            {
                using (connection =
                           new NpgsqlConnection(
                               "Server=127.0.0.1;Port=5438;Database=testDb;Username=postgres;Password=12345678;"))
                {
                    connection.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userLogin);
                        foreach (string rightId in rightIds)
                        {
                            string roleId = Regex.Match(rightId, @"\d+").Value;
                            int roleIdInt = Convert.ToInt32(roleId);
                            
                            command.Parameters.AddWithValue("@roleId", roleIdInt);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (NpgsqlException err)
            {
                Logger.Error("Произошла ошибка: " + err.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        public void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            string query = "DELETE FROM \"TestTaskSchema\".\"UserRequestRight\" WHERE \"userId\" = @userId AND \"rightId\" = @rightId";
            NpgsqlConnection connection = null;

            try
            {
                using (connection =
                           new NpgsqlConnection(
                               "Server=127.0.0.1;Port=5438;Database=testDb;Username=postgres;Password=12345678;"))
                {
                    connection.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        foreach (string rightId in rightIds)
                        {
                            string roleId = Regex.Match(rightId, @"\d+").Value;
                            int roleIdInt = Convert.ToInt32(roleId);
                            
                            command.Parameters.AddWithValue("@userId", userLogin);
                            command.Parameters.AddWithValue("@rightId", Convert.ToInt32(roleIdInt));
                            
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (NpgsqlException err)
            {
                Logger.Error("Произошла ошибка: " + err.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        public IEnumerable<string> GetUserPermissions(string userLogin)
        {
            string query = "SELECT * FROM \"TestTaskSchema\".\"UserRequestRight\" WHERE \"userId\" = @userId";
            
            NpgsqlConnection connection = null;
            List<string> userPermission = new List<string>();
            
            try
            {
                using (connection =
                           new NpgsqlConnection(
                               "Server=127.0.0.1;Port=5438;Database=testDb;Username=postgres;Password=12345678;"))
                {
                    connection.Open();

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userLogin);

                        NpgsqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            string userId = reader["userId"].ToString();
                            string rightId = reader["rightId"].ToString();
                            
                            string userRight = $"{userId}:{rightId}";
                            
                            userPermission.Add(userRight);
                            //userPermission.Add(rightId);
                            
                            // Dictionary<string, string> userPerm = new Dictionary<string, string>()
                            // {
                            //     ["userId"] = reader["userId"].ToString(),
                            //     ["rightId"] = reader["rightId"].ToString()
                            // };
                         
                            //userPermission.Add(userId);
                        }
                    }
                }
            }
            catch (NpgsqlException err)
            {
                Logger.Error("Произошла ошибка: " + err.Message);
            }
            finally
            {
                connection.Close();
            }

            return userPermission;
        }

        public ILogger Logger { get; set; }
    }
}