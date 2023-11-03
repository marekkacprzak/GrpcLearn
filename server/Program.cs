// See https://aka.ms/new-console-template for more information

using Greet;
using Grpc.Core;
using Grpc.Reflection;
using Grpc.Reflection.V1Alpha;
using server;
using Sqrt;
using Sum;

const int Port = 50051;
var serverCrt = File.ReadAllText("ssl/server.crt");
var serverKey = File.ReadAllText("ssl/server.key");
var keypair = new KeyCertificatePair(serverCrt, serverKey);
var caCrt = File.ReadAllText("ssl/ca.crt");
var channelCredentials = new SslServerCredentials(
    new List<KeyCertificatePair>() { keypair }, 
    caCrt, true);
var reflectionServiceImpl = new ReflectionServiceImpl(GreetingService.Descriptor, ServerReflection.Descriptor);
Server? server = null;
try
{
    server = new Server
    { 
        Services =
        {
            GreetingService.BindService(new GreetingServiceImpl()),
            ServerReflection.BindService(reflectionServiceImpl)
        },
        //Services = { SumOfNumbersService.BindService(new SumOfNumbersServiceImpl()) },
        //Services = { SqrtService.BindService(new SqrtServiceImpl()) },
        Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
        //Ports = { new ServerPort("localhost", Port, channelCredentials) }
    };
    server.Start();
    Console.WriteLine($"The server is listening on the port {Port}");
    Console.ReadKey();
}
catch (Exception e) when (e is IOException ex)
{
    Console.WriteLine($"The server failed to start {ex.Message}");
    throw;
}
finally
{
    if (server != null)
        await server.ShutdownAsync();
}
