using System.Text;
using Grpc.Core;
using Sum;

namespace server;

public class SumOfNumbersServiceImpl:SumOfNumbersService.SumOfNumbersServiceBase
{
    public override Task<SumResponse> Sum(SumRequest request, ServerCallContext context)
    {
        var result=request.SumOfNumbers.First + request.SumOfNumbers.Second;
        return Task.FromResult(new SumResponse()
        {
            Result = result
        });
    }
    
    public override async Task<NumberResponse> ComputeAverage(IAsyncStreamReader<NumberRequest> requestStream, ServerCallContext context)
    {
        var sum = 0d;
        var count = 0;
        while (await requestStream.MoveNext())
        {
            sum += requestStream.Current.Number;
            count++;
        }
        var result = sum / count;
        return new NumberResponse()
        {
            Result = result
        };
    }

    public override async Task CurrentMaximum(IAsyncStreamReader<NumberRequest> requestStream,
        IServerStreamWriter<MaximuNumberResponse> responseStream, ServerCallContext context)
    {
        var currentMax = 0;
        while (await requestStream.MoveNext())
        {
            Console.WriteLine($"Recive {requestStream.Current.Number}");
            if (currentMax < requestStream.Current.Number)
            {
                currentMax = requestStream.Current.Number;
                Console.WriteLine($"Sending {currentMax}");
                await responseStream.WriteAsync(new MaximuNumberResponse()
                {
                    Number = currentMax
                });
            }
        }
    }
}