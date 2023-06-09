using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using MySql.Data.MySqlClient;

namespace DemoApi.Controllers
{
    public class CountryController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        private static Random random = new Random();
        private static string cacheKey = "Country";
        public CountryController(IConfiguration configuration,
                                 IDistributedCache cache)
        {
            _cache = cache;
            _configuration = configuration;
            
        }
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public async Task<List<Country>> GetDataAsync()
        {
            var result = new List<Country>();
            string query = @"SELECT * FROM country";
            string sqlDataSource = _configuration.GetConnectionString("ExamDB");
            using(MySqlConnection connection = new MySqlConnection(sqlDataSource))
            {
                await connection.OpenAsync();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        result.Add(new Country()
                        {
                            Id = Convert.ToInt32(reader["country_id"]),
                            CountryName = reader["country"].ToString()
                        });
                    }
                    await connection.CloseAsync(); 
                }
            }
            return result;
        }
        [HttpGet]
        [Route("GetCountries")]
        public async Task<IActionResult> GetCountries()
        {
            
            // Trying to get data from the Redis cache
            byte[] cachedData = await _cache.GetAsync(cacheKey);
            List<Country> countries = new();
            if(cachedData != null)
            {
                var cachedDataString = Encoding.UTF8.GetString(cachedData);
                countries = JsonSerializer.Deserialize<List<Country>>(cachedDataString);
            }
            else
            {
                countries = await GetDataAsync();
                var cachedDataString = JsonSerializer.Serialize(countries);
                var dataToCache = Encoding.UTF8.GetBytes(cachedDataString);
                
                var cachePptions = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Now.AddMinutes(5))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(3));

                // Add the data into the cache
                await _cache.SetAsync(cacheKey, dataToCache, cachePptions);
            }
            
            return new JsonResult(countries);
        }
        [HttpGet]
        [Route("InsertData")]
        public async Task<IActionResult> InsertData()
        {
           
            try
            {
                string query = "insert into country(country_id,country,last_update) values('" +random.Next(200, 1000)+ "','" +RandomString(5)+ "','" +DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")+ "');";
                string sqlDataSource = _configuration.GetConnectionString("ExamDB");
                using(MySqlConnection connection = new MySqlConnection(sqlDataSource))
                {
                    await connection.OpenAsync();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        var reader = command.ExecuteReader();
                        await connection.CloseAsync(); 
                    }
                }
                await _cache.RemoveAsync(cacheKey);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
    }
}