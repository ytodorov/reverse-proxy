namespace SampleApi
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        public string Pk => Guid.NewGuid().ToString();
        public string? Name { get; set; }
        public decimal Price { get; set; }

        public string CreatedOnMachine => Environment.MachineName;

        public DateTime CreatedOnUtc => DateTime.UtcNow;
    }
}
