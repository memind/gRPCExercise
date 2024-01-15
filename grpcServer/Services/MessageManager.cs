using Google.Protobuf;
using Grpc.Core;
using grpcMessageServer;

namespace grpcServer.Services
{
    public class MessageManager : MessageService.MessageServiceBase
    {
        public override async Task<MessageResponse> SendMessage(MessageRequest request, ServerCallContext context)
        {
            await Console.Out.WriteLineAsync($"REQUEST\n{request.Name}: {request.Message}");
            MessageResponse response = new MessageResponse() { Message = request.Message };

            return response;
        }

        public override async Task SendMessageServerStreaming(MessageRequest request, IServerStreamWriter<MessageResponse> responseStream, ServerCallContext context)
        {
            await Console.Out.WriteLineAsync($"{request.Name}: {request.Message}");

            for (int i = 1; i <= 20; i++)
            {
                await Task.Delay(500);

                await responseStream.WriteAsync(new MessageResponse
                {
                    Message = $"{i}. Stream"
                });
            }
        }

        public override async Task<MessageResponse> SendMessageClientStreaming(IAsyncStreamReader<MessageRequest> requestStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
                await Console.Out.WriteLineAsync($"{requestStream.Current.Name}: {requestStream.Current.Message}");
            
            return new() { Message = "Stream veri alindi" };
        }

        public override async Task SendMessageBiDirectionalStreaming(IAsyncStreamReader<MessageRequest> requestStream, IServerStreamWriter<MessageResponse> responseStream, ServerCallContext context)
        {
            var task = Task.Run(async () =>
            {
                while (await requestStream.MoveNext())
                {
                    await Task.Delay(500);
                    await Console.Out.WriteLineAsync($"{requestStream.Current.Name}: {requestStream.Current.Message}");
                }
            });

            for (int counter = 0; counter < 10; counter++)
            {
                await Task.Delay(500);
                await responseStream.WriteAsync(new MessageResponse { Message = $"{counter+1}. Streaming From Server"});
            }

            await task;
        }
    }
}
