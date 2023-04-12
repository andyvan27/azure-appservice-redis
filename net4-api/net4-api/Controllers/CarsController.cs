using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using net4_api.Models;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Web.Http;

namespace net4_api.Controllers
{
    public class RedisCacheFactory
    {
        private readonly Lazy<ConnectionMultiplexer> _connectionMultiplexer;
        private readonly object _locker = new object();
        private IDistributedCache _redisCache;

        public IDistributedCache GetRedisCache()
        {
            if (_redisCache == null)
            {
                lock (_locker)
                {
                    if (_redisCache == null)
                    {   
                        _redisCache = new RedisCache(new RedisCacheOptions
                        {
                            Configuration = ConfigurationManager.ConnectionStrings["RedisConnection"].ConnectionString,
                            InstanceName = "DistRedis:"
                        });
                    }
                }
            }
            return _redisCache;
        }
    }

    public class CarsController : ApiController
    {
        private string CACHE_KEY = "Cars";
        private int CACHING_MINUTES = 1;

        //No cache
        public async Task<IEnumerable<Car>> GetNoCache()
        {
            return await GetCars();
        }

        //Mem cache
        public async Task<IEnumerable<Car>> GetMemCache()
        {
            IEnumerable<Car> cars = null;
            if(!MemoryCache.Default.Contains(CACHE_KEY))
            {
                cars = await GetCars();
                MemoryCache.Default.Add(CACHE_KEY, cars, DateTimeOffset.Now.AddMinutes(CACHING_MINUTES));
            }
            return (IEnumerable<Car>)MemoryCache.Default.Get(CACHE_KEY);
        }

        //Redis as a distributed cache
        private static RedisCacheFactory _distFactory = null;
        private static RedisCacheFactory DistFactory
        {
            get
            {
                if(_distFactory == null)
                {
                    _distFactory = new RedisCacheFactory();
                }
                return _distFactory;
            }
        }

        public async Task<IEnumerable<Car>> GetDistCache()
        {
            IEnumerable<Car> cars;            
            string json = await DistFactory.GetRedisCache().GetStringAsync(CACHE_KEY);
            if (!string.IsNullOrEmpty(json))
            {
                cars = JsonConvert.DeserializeObject<IEnumerable<Car>>(json);
            }
            else
            {
                cars = await GetCars();
                await DistFactory.GetRedisCache().SetStringAsync(CACHE_KEY, JsonConvert.SerializeObject(cars), new DistributedCacheEntryOptions() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(CACHING_MINUTES) });
            }
            return cars;
        }

        //Redis Multiplexer
        private static IDatabase _redisDB = null;
        private static IDatabase RedisDB
        {
            get
            {
                if (_redisDB == null)
                {
                    _redisDB = ConnectionMultiplexer.Connect(ConfigurationManager.ConnectionStrings["RedisConnection"].ConnectionString).GetDatabase();                    
                }
                return _redisDB;
            }
        }

        public async Task<IEnumerable<Car>> GetRedisCache()
        {
            IEnumerable<Car> cars;
            string json = await RedisDB.StringGetAsync(CACHE_KEY);
            if (!string.IsNullOrEmpty(json))
            {
                cars = JsonConvert.DeserializeObject<IEnumerable<Car>>(json);
            }
            else
            {
                cars = await GetCars();
                await RedisDB.StringSetAsync(CACHE_KEY, JsonConvert.SerializeObject(cars), TimeSpan.FromMinutes(CACHING_MINUTES));
            }
            return cars;
        }

        private async Task<IEnumerable<Car>> GetCars()
        {
            await Task.Delay(10000); //10 seconds load
            return new List<Car>()
            {
                new Car{ Make = "Toyota", Model = "Landcruiser" },
                new Car{ Make = "Nissan", Model = "Patroll" },
                new Car{ Make = "Mazda", Model = "BT50" },
                new Car{ Make = "Mitsubishi", Model = "Pajero" },
                new Car{ Make = "Mercedes", Model = "G63" },
                new Car{ Make = "BMW", Model = "X7" }
            };
        }
    }
}