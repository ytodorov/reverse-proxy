namespace ReverseProxyApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Bootstrapper.StartLoadBalancer(args);
        }
    }
}