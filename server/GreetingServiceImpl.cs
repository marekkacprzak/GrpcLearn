using System.Text;
using Greet;
using Grpc.Core;

namespace server;

public class GreetingServiceImpl : GreetingService.GreetingServiceBase
{
    public override Task<GreetingResponse> Greet(GreetingRequest request, ServerCallContext context)
    {
        var result = $"hello {request.Greeting.FirstName} {request.Greeting.LastName}";
        return Task.FromResult(new GreetingResponse()
        {
            Result = result
        });
    }

    public override async Task GreetManyTimes(GreetManyTimesRequest request, IServerStreamWriter<GreetManyTimesResponse> responseStream, ServerCallContext context)
    {
        Console.WriteLine($"The server recive {request}");
        var result = $"hello {request.Greeting.FirstName} {request.Greeting.LastName}";
        await foreach (var i in FetchItems())//Enumerable.Range(1,10)
        {
            await responseStream.WriteAsync(new GreetManyTimesResponse()
            {
                Result = result
            });
        }
    }

    public override async Task<LongGreetingResponse> LongGreet(IAsyncStreamReader<LongGreetingRequest> requestStream, ServerCallContext context)
    {
        //var result = $"hello {request.Greeting.FirstName} {request.Greeting.LastName}";
        StringBuilder str = new StringBuilder();
        while (await requestStream.MoveNext())
        {
            str.AppendLine($"hello {requestStream.Current.Greeting.FirstName} {requestStream.Current.Greeting.LastName}");
        }
        return new LongGreetingResponse()
        {
            Result = str.ToString()
        };
    }

    public override async Task GreetEveryone(IAsyncStreamReader<GreetEveryoneRequest> requestStream,
        IServerStreamWriter<GreetEveryoneResponse> responseStream, ServerCallContext context)
    {
        while (await requestStream.MoveNext())
        {
            var str = $"hello {requestStream.Current.Greeting.FirstName} {requestStream.Current.Greeting.LastName}";
            Console.WriteLine($"Sending: {str}");
            await responseStream.WriteAsync(new GreetEveryoneResponse()
            {
                Result = str
            });
        }
    }

    public override async Task<GreetingResponse> Great_with_deadline(GreetingRequest request, ServerCallContext context)
    {
        await Task.Delay(300);
        var result = $"hello {request.Greeting.FirstName} {request.Greeting.LastName}";
        return await Task.FromResult(new GreetingResponse()
        {
            Result = result
        });
    }

    static async IAsyncEnumerable<int> FetchItems()
    {
        for (int i = 1; i <= 10; i++)
        {
            await Task.FromResult(i);
            yield return i;
        }
    }
}