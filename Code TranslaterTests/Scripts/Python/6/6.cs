
string IP = "127.0.0.1";
float PORT = 25565;

var server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM);
server_sock.bind(IP, PORT);
server_sock.listen();
var connaddr = server_sock.accept();
var conn = connaddr.conn;
var addr = connaddr.addr;

static void recv()
{
	while (true)
	{
		var income_message = conn.recv(1024);
		Console.WriteLine("client: " + income_message.decode());
	}
}

static void send()
{
	while (true)
	{
		var message = input("");
		conn.send(message.encode());
		if (message == "break")
		{
			break;
		}
	}
}

static void main()
{
	var x = threading.Thread(recv, null);
	x.start();
	var y = threading.Thread(send, null);
	y.start();
	
	Console.WriteLine("started threads!");
	if (send() == false)
	{
		x.join();
		y.join();
		server_sock.close();
		Console.WriteLine("stop");
	}
}

if (__name__ == "__main__")
{
	main();
}