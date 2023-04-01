
using Core.Models.Sender;
using System.Text.Json;
using System.Threading.Tasks;
using WebserverService;

var webServer = new WebServer("http://localhost:5003", "https://localhost:5004");
var t1 = webServer.RunAsync();

var game = new Game
{
    Clock = "11:55"

};

var guest = new Guest
{
    Score = "10",
     Name = "FCB",
};

var home = new Home
{
    Score = "99",
    Name = "ALBA",
};

var board = new Board
{
    Ticker = "Das ist ein weiterer Test.",
    Game = game,
    Guest= guest,
    Home = home,
};

var serializeOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

var message = JsonSerializer.Serialize(board, serializeOptions);

var webSocket = new WebSocket("http://localhost:9000", default);
webSocket.Set(message);

var t2 = webSocket.RunAsync();

await Task.WhenAll(t1, t2);
