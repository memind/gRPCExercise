using Grpc.Net.Client;
using grpcMessageClient;
using grpcFileTransportClient;
using Google.Protobuf;


var channel = GrpcChannel.ForAddress("https://localhost:5000");
var messageClient = new MessageService.MessageServiceClient(channel);
var fileClient = new FileService.FileServiceClient(channel);

var input = "";

while (input != "exit")
{
    switch (input)
    {
        case "DataTransportTypes":
            Unary(messageClient);
            ServerStreaming(messageClient);
            ClientStreaming(messageClient);
            BiDirectionalStreaming(messageClient);
            break;

        case "upload":
            FileUpload(fileClient);
            break;

        case "download":
            FileDownload(fileClient);
            break;

        default:
            break;
    }

    Console.Write("Choose an Operation: ");
    input = Console.ReadLine();
}

async void FileDownload(FileService.FileServiceClient fileClient)
{
    string downloadPath = @""; // destination folder path to download to

    var fileInfo = new grpcFileTransportClient.FileInfo()
    {
        FileExtension = ".txt",
        FileName = "TEST"
    };

    FileStream stream = null;

    var download = fileClient.FileDownload(fileInfo);
    int count = 0;
    decimal chunkSize = 0;

    while (await download.ResponseStream.MoveNext(new CancellationToken()))
    {
        if (count++ == 0)
        {
            stream = new FileStream($@"{downloadPath}\{download.ResponseStream.Current.Info.FileName}{download.ResponseStream.Current.Info.FileExtension}", FileMode.CreateNew);
            stream.SetLength(download.ResponseStream.Current.FileSize);
        }

        var buffer = download.ResponseStream.Current.Buffer.ToByteArray();
        await stream.WriteAsync(buffer, 0, download.ResponseStream.Current.ReadedByte);

        await Console.Out.WriteLineAsync($"{Math.Round((chunkSize += download.ResponseStream.Current.ReadedByte) * 100) / download.ResponseStream.Current.FileSize}%");
    }

    await Console.Out.WriteLineAsync("Downloaded");
    await stream.DisposeAsync();
    stream.Close();
}

async void FileUpload(FileService.FileServiceClient fileClient)
{
    await Console.Out.WriteAsync("File: ");
    string file = Console.ReadLine();

    using FileStream stream = new FileStream(file, FileMode.Open);

    var content = new BytesContent()
    {
        FileSize = stream.Length,
        ReadedByte = 0,
        Info = new grpcFileTransportClient.FileInfo { FileName = Path.GetFileNameWithoutExtension(stream.Name), FileExtension = Path.GetExtension(stream.Name) }
    };

    var upload = fileClient.FileUpload();
    byte[] buffer = new byte[2048];

    while ((content.ReadedByte = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
        content.Buffer = ByteString.CopyFrom(buffer);
        await upload.RequestStream.WriteAsync(content);
    }

    await upload.RequestStream.CompleteAsync();
    stream.Close();
}

async void BiDirectionalStreaming(MessageService.MessageServiceClient messageClient)
{
    var biDirectionalRequest = messageClient.SendMessageBiDirectionalStreaming();
    var task = Task.Run(async () =>
    {
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(500);
            await biDirectionalRequest.RequestStream.WriteAsync(new MessageRequest() { Name = "Memin", Message = $"{i + 1}. Streaming From Client" });
        }
    });

    while (await biDirectionalRequest.ResponseStream.MoveNext(new CancellationToken()))
        Console.WriteLine(biDirectionalRequest.ResponseStream.Current.Message);

    await task;
    await biDirectionalRequest.RequestStream.CompleteAsync();
}

async void Unary(MessageService.MessageServiceClient messageClient)
{
    Console.WriteLine("UNARY MESSAGING");
    var unaryReq = new MessageRequest() { Name = "Memin", Message = "UNARY" };
    var unaryRes = await messageClient.SendMessageAsync(unaryReq);

    Console.WriteLine($"Response: {unaryRes.Message}");
}

async void ServerStreaming(MessageService.MessageServiceClient messageClient)
{
    Console.WriteLine("SERVER STREAMING MESSAGING");

    var serverStreamingRequest = new MessageRequest() { Name = "Memin", Message = "SERVER STREAMING" };
    var serverStreamingResponse = messageClient.SendMessageServerStreaming(serverStreamingRequest);

    while (await serverStreamingResponse.ResponseStream.MoveNext(new CancellationToken()))
        Console.WriteLine(serverStreamingResponse.ResponseStream.Current.Message);
}

async void ClientStreaming(MessageService.MessageServiceClient messageClient)
{
    Console.WriteLine("CLIENT STREAMING MESSAGING");
    var clientStreamingRequest = messageClient.SendMessageClientStreaming();

    for (int i = 0; i < 10; i++)
    {
        await Task.Delay(1000);
        await clientStreamingRequest.RequestStream.WriteAsync(new MessageRequest()
        {
            Name = "Memin",
            Message = $"Client Streaming Message {i + 1}"
        });
    }

    await clientStreamingRequest.RequestStream.CompleteAsync();

    Console.WriteLine((await clientStreamingRequest.ResponseAsync).Message);

}