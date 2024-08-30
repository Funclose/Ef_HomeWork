using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;

namespace Ef_HomeWork
{
     class Program
    {
        static async Task Main(string[] args)
        {
            var databaseServer = new DatabaseService();
            await databaseServer.EnsurePopulate();
            
            var options = databaseServer.GetContextOptions();
            using (var context = new ApplicationContext(options))
            {
                
                var orderService = new OrderService(context);

                
                var orders = await orderService.GetAllOrders();

                foreach (var order in orders)
                {
                    Console.WriteLine($"Order {order.Id} made on {order.OrderData} contains the following products:");
                    foreach (var product in order.Products)
                    {
                        Console.WriteLine($"- {product.Name}, Price: {product.Price}");
                    }
                }
            }
        }
    }

    public class SampleContextFactory : IDesignTimeDbContextFactory<ApplicationContext>
    {
        public ApplicationContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationContext>();

            // получаем конфигурацию из файла appsettings.json
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            IConfigurationRoot config = builder.Build();

            // получаем строку подключения из файла appsettings.json
            string connectionString = config.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);
            return new ApplicationContext(optionsBuilder.Options);
        }
    }
    public class Product 
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Decimal Price { get; set; }

        public int? OrderId { get; set; }
        public Order? Orders { get; set; }

        public static Product[] TestData() => new Product[]
        {
            new Product
            {
                Name = "Test",
                Price = 2.3M
            },
            new Product
            {
                Name = "Tester2",
                Price = 5
            }
        };
    }

    public class Order
    { 
        public int Id { get; set; }
        public DateTime OrderData { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<Product> Products { get; set; }= null;
        public DbSet<Order> Orders { get; set; }=null;

        public ApplicationContext(DbContextOptions<ApplicationContext> options) :base(options)
        {

        }


        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlServer("Server=DESKTOP-Q2JP8KP;Database=Shop;Trusted_Connection=True;TrustServerCertificate=True;");
        //}
    }
    public class DatabaseService
    { 
        public DbContextOptions<ApplicationContext> GetContextOptions()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json");
            var config = builder.Build();
            string connectionString = config.GetConnectionString("DefaultConnection");
            var optionBuilder = new DbContextOptionsBuilder<ApplicationContext>();
            return optionBuilder.UseSqlServer(connectionString).Options;
        }

        public async Task EnsurePopulate()
        {
            using(var db = new ApplicationContext(GetContextOptions()))
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                db.Products.AddRange(Product.TestData());
                 db.SaveChangesAsync();

                var orderService = new OrderService(db);

                // Example usage: Add an order
                var order = new Order
                {
                    OrderData = DateTime.Now,
                    Products = new List<Product> { db.Products.First() }
                };
                await orderService.AddOrder(order);

                // Example usage: Get all orders
                var orders = await orderService.GetAllOrders();
                foreach (var ord in orders)
                {
                    Console.WriteLine($"Order {ord.Id} has {ord.Products.Count} products.");
                }

            }
        }

    }

    public class OrderService
    {
        private readonly ApplicationContext _context;

        public OrderService(ApplicationContext context)
        {
            _context = context;
        }

        public async Task AddOrder(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        public async Task<Order> GetOrderById(int id)
        {
            return await _context.Orders.Include(o => o.Products).FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Order>> GetAllOrders()
        {
            return await _context.Orders.Include(o => o.Products).ToListAsync();
        }
    }
}
