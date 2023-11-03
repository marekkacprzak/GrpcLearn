// See https://aka.ms/new-console-template for more information

using Dummy;
using Greet;
using Grpc.Core;
using Sqrt;
using Sum;

//const string target = "127.0.0.1:50051";

var clientCrt = File.ReadAllText("ssl/client.crt");
var clientKey = File.ReadAllText("ssl/client.key");
var caCrt = File.ReadAllText("ssl/ca.crt");
var channelCredentials = new SslCredentials(caCrt, new KeyCertificatePair(clientCrt, clientKey));

var channel = new Channel("localhost", 50051, channelCredentials);
//var channel = new Channel(target, ChannelCredentials.Insecure);

await channel.ConnectAsync().ContinueWith((task) =>
{
    if (task.Status==TaskStatus.RanToCompletion)
        Console.WriteLine("The client connected successfully");
});
//1. step dummy
//var client = new DummyService.DummyServiceClient(channel);


var client = new GreetingService.GreetingServiceClient(channel);
var clientCalc = new SumOfNumbersService.SumOfNumbersServiceClient(channel);
var clientSqrt = new SqrtService.SqrtServiceClient(channel);

var greeting = new Greeting()
{
    FirstName = "Marek",
    LastName = "Kacprzak"
};
//2.
//DoSimpleGreet(client,greeting);
//3.
//DoSimpleCalculation(clientCalc);
//4.
//await DoManyGreetings(client, greeting);
//5.
//await DoLongGreet(client, greeting);
//6.
//await ComputeAverage(clientCalc);
//7. biDirectional stream
//await DoGreetEveryone(client, greeting);
//7. biDirectional stream
//await CurrentMaximum(clientCalc);
//8. Exception handling
//await SqrtClient(clientSqrt, -1);
//9. DEadline handling
await DoSimpleGreetWithDeadline(client, greeting, 500);

// disposing:
await channel.ShutdownAsync().ConfigureAwait(false);
Console.ReadKey();

//2. step unary
static void DoSimpleGreet(GreetingService.GreetingServiceClient client, Greeting greeting)
{
    var response = client.Greet(new GreetingRequest()
    {
        Greeting = greeting
    });
    Console.WriteLine(response.Result);
}
//3. step unary testcase
static void DoSimpleCalculation(SumOfNumbersService.SumOfNumbersServiceClient client)
{
    var sum = new SumOfNumbers()
    {
        First = 3,
        Second = 10
    };
    var response = client.Sum(new SumRequest()
    {
        SumOfNumbers = sum
    });
    Console.WriteLine(response.Result);
}
//4. step server streaming
static async Task DoManyGreetings(GreetingService.GreetingServiceClient client, Greeting greeting)
{
    var response = client.GreetManyTimes(new GreetManyTimesRequest()
    {
        Greeting = greeting
    });
    while (await response.ResponseStream.MoveNext().ConfigureAwait(false))
    {
        Console.WriteLine(response.ResponseStream.Current.Result);
        await Task.Delay(200).ConfigureAwait(false);
    }
}
//5. stream client
static async Task DoLongGreet(GreetingService.GreetingServiceClient client, Greeting greeting)
{
    var request = new LongGreetingRequest()
    {
        Greeting = greeting
    };
    var stream = client.LongGreet();
    await foreach (var i in FetchItems(10))
    {
        await stream.RequestStream.WriteAsync(request).ConfigureAwait(false);
    }
    await stream.RequestStream.CompleteAsync().ConfigureAwait(false);
    var response = await stream.ResponseAsync;
    Console.WriteLine(response.Result);
}
//6. test case
static async Task ComputeAverage(SumOfNumbersService.SumOfNumbersServiceClient client)
{
    var clientStream = client.ComputeAverage();
    await foreach (var i in FetchItems(4))
    {
        await clientStream.RequestStream.WriteAsync(new NumberRequest()
        {
            Number = i
        });
    }

    await clientStream.RequestStream.CompleteAsync();
    var response = await clientStream.ResponseAsync;
    Console.WriteLine(response.Result);
}
//7. biDirectional stream
static async Task DoGreetEveryone(GreetingService.GreetingServiceClient client, Greeting greeting)
{
    var request = new GreetEveryoneRequest()
    {
        Greeting = greeting
    };
    var stream = client.GreetEveryone();
    await foreach (var i in FetchItems(10))
    {
        Console.WriteLine($"Sending: {request}");
        await stream.RequestStream.WriteAsync(request).ConfigureAwait(false);
    }
    var responseTask = Task.Run(async () =>
    {
        while (await stream.ResponseStream.MoveNext().ConfigureAwait(false))
        {
            Console.WriteLine($"Recive: {stream.ResponseStream.Current.Result}");
        }
    });
    await stream.RequestStream.CompleteAsync().ConfigureAwait(false);
    await responseTask;
}
//7. biDirectional stream ( two tasks)
static async Task DoGreetEveryone2(GreetingService.GreetingServiceClient client, Greeting greeting)
{
    var request = new GreetEveryoneRequest()
    {
        Greeting = greeting
    };
    var stream = client.GreetEveryone();
    var requestTask = Task.Run(async () =>
    {
        await foreach (var i in FetchItems(10))
        {
            Console.WriteLine($"Sending: {request}");
            await stream.RequestStream.WriteAsync(request).ConfigureAwait(false);
        }

        await stream.RequestStream.CompleteAsync().ConfigureAwait(false);
    });
    var responseTask = Task.Run(async () =>
    {
        while (await stream.ResponseStream.MoveNext().ConfigureAwait(false))
        {
            Console.WriteLine($"Recive: {stream.ResponseStream.Current.Result}");
        }
    });
    await Task.WhenAll(requestTask, responseTask);
    await responseTask;
}
//7. test case
static async Task CurrentMaximum(SumOfNumbersService.SumOfNumbersServiceClient client)
{
    int[] sample = { 1, 5, 3, 6, 2, 20 };
    var clientStream = client.CurrentMaximum();
    await foreach (var i in FetchFromArray(sample))
    {
        Console.WriteLine($"Sending: {i}");
        await clientStream.RequestStream.WriteAsync(new NumberRequest()
        {
            Number = i
        }).ConfigureAwait(false);
    }
    var responseTask = Task.Run(async () =>
    {
        while (await clientStream.ResponseStream.MoveNext().ConfigureAwait(false))
        {
            Console.Write($"{clientStream.ResponseStream.Current.Number},");
        }
    });
    await clientStream.RequestStream.CompleteAsync();
    await responseTask;
}

//8. Exception handling
static async Task SqrtClient(SqrtService.SqrtServiceClient clientSqrt, int number)
{
    try
    { 
        var response = await clientSqrt.sqrtAsync(new SqrtRequest()
        {
            Number = number
        }).ConfigureAwait(false);
        Console.WriteLine(response.SqrtRoot);
    }
    catch(RpcException e)
    {
        Console.WriteLine($"Error: {e.Status.Detail}");
    }
}

//9. DEadline handling
static async Task DoSimpleGreetWithDeadline(GreetingService.GreetingServiceClient client, Greeting greeting, double timeout )
{
    try
    { 
        var response = await client.Great_with_deadlineAsync(new GreetingRequest()
        {
            Greeting = greeting
        }, deadline: DateTime.UtcNow.AddMilliseconds(timeout))
            .ConfigureAwait(false);
        Console.WriteLine(response.Result);
    }
    catch(RpcException e) when(e.StatusCode==StatusCode.DeadlineExceeded)
    {
        Console.WriteLine($"Error: {e.Status.Detail}");
    }
}
//Helper
static async IAsyncEnumerable<int> FetchItems(int count)
{
    for (int i = 1; i <= count; i++)
    {
        await Task.FromResult(i).ConfigureAwait(false);
        yield return i;
    }
}

static async IAsyncEnumerable<int> FetchFromArray(int[] numbers)
{
    foreach (var t in numbers)
    {
        await Task.FromResult(t).ConfigureAwait(false);
        yield return t;
    }
}


