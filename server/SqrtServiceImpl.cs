using Grpc.Core;
using Sqrt;

namespace server;

public class SqrtServiceImpl:SqrtService.SqrtServiceBase
{
    public override async Task<SqrtResponse> sqrt(SqrtRequest request, ServerCallContext context)
    {
        var number = request.Number;
        if (number > 0)
        {
            return await Task.FromResult(new SqrtResponse()
            {
                SqrtRoot = Math.Sqrt(number)
            });
        }
        throw new RpcException(new Status(StatusCode.InvalidArgument,"number < 0"));
    }
}