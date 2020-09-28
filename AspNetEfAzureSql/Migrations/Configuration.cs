namespace AspNetEfAzureSql.Migrations
{
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<AspNetEfAzureSql.Data.BorkContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(AspNetEfAzureSql.Data.BorkContext context)
        {
        }
    }
}