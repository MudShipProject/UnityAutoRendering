namespace AutoRenderingService;

public class AgentEntry
{
    public string IpAddress { get; set; }
    public int Port { get; set; }

    public AgentEntry(string ipAddress, int port)
    {
        IpAddress = ipAddress;
        Port = port;
    }

    public override string ToString() => $"{IpAddress} : {Port}";
}
