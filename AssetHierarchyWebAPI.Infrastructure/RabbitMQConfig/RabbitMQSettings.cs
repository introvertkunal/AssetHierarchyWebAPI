namespace AssetHierarchyWebAPI.Infrastructure.RabbitMQConfig
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; }

        public string UserName { get; set; }    

        public string Password { get; set; }

        public string InputQueue { get; set; }

        public string  ResultQueue { get; set; }


        public RabbitMQSettings() {

            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest";
            InputQueue = Environment.GetEnvironmentVariable("RABBITMQ_INPUTQUEUE") ?? "queue";
            ResultQueue = Environment.GetEnvironmentVariable("RABBITMQ_RESULTQUEUE") ?? "queue";


        }
    }
}
