using CatalogService.Api.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CatalogService.Api.Extensions
{
    public static class DbContextRegistiration
    {
        public static void ConfigureDbContext(this IServiceCollection services)
        {
            services.AddDbContext<CatalogContext>(options => 
            options.UseSqlServer(Configuration.ConnectionString));        
        }
    }

    public static class Configuration
    {
        static public string ConnectionString
        {
            get
            {
                ConfigurationManager cfg = new ConfigurationManager();
                cfg.SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../../Services/CatalogService/CatalogService.Api"));
                cfg.AddJsonFile("appsettings.json");

                return cfg.GetConnectionString("MicrosoftSQL");
            }
        }
    }
}
