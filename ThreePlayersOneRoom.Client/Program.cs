using System.Net.Http.Headers;

var baseUrl = args[0];
var playerId = Guid.Parse(args[1]);

HttpClient http = new()
{
    BaseAddress = new Uri(baseUrl),
    DefaultRequestHeaders =
    {
        Authorization = AuthenticationHeaderValue.Parse($"Bearer {playerId}")
    }
};


while (Console.ReadLine() is { } command)
{
    var split = command.Split(' ');
    var arg0 = split[0];

    switch (arg0)
    {
        case "c":
            _ = StreamEvents();
            break;
        case "o":
            await OpenRoom();
            break;
        case "j" when split.Length > 1:
            var roomId = int.Parse(split[1]);
            await JoinRoom(roomId);
            break;
        default:
            Console.WriteLine("Unknown command or invalid args count.");
            break;
    }
}

async Task<bool> OpenRoom()
{
    var resp = await http.PostAsync("rooms", null);
    var respContent = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
    {
        Console.WriteLine($"Failed to open room: {respContent}, code: {resp.StatusCode}");
        return false;
    }

    var roomId = int.Parse(respContent);
    Console.WriteLine($"Opened room {roomId}");
    return true;
}

async Task<bool> JoinRoom(int roomId)
{
    var resp = await http.PostAsync($"rooms/{roomId}/join", null);
    var respContent = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
    {
        Console.WriteLine($"Failed to join room: {respContent}, code: {resp.StatusCode}");
        return false;
    }

    Console.WriteLine($"Joined room {roomId}");
    return true;
}

async Task StreamEvents()
{
    var stream = await http.GetStreamAsync("sse");
    Console.WriteLine("Streaming events");
    StreamReader sr = new(stream);
    try
    {
        while (!sr.EndOfStream)
        {
            var line = sr.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
                Console.WriteLine($"SSE: {line}");
        }
    }
    catch (IOException)
    {
    }
    finally
    {
        Console.WriteLine("Stream ended");
    }
}