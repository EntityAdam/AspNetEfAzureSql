using System.Data.Entity;

namespace AspNetEfAzureSql.Data
{
    public class BorkContext : DbContext
    {
        public BorkContext() : base("name=BorkContext")
        {
        }

        public System.Data.Entity.DbSet<AspNetEfAzureSql.Bork> Borks { get; set; }
    }
}