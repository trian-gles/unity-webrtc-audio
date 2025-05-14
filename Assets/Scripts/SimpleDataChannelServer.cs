using UnityEngine;
using System.Net.Sockets;
using System.Net;
using WebSocketSharp;
using WebSocketSharp.Server;

public class SimpleDataChannelServer : MonoBehaviour
{
    private WebSocketSharp.Server.WebSocketServer ws;
    //private WebSocketServer wssv;
    private string serverIpv4Address;
    private int serverPort = 8080;

    public Debugger debug;

    private void Awake()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                serverIpv4Address = ip.ToString();
                break;
            }
        }
        
        ws = new WebSocketSharp.Server.WebSocketServer($"ws://{serverIpv4Address}:{serverPort}");
        ws.AddWebSocketService<SimpleDataChannelService>($"/{nameof(SimpleDataChannelService)}");
        debug.Log($"About to start server on ip {serverIpv4Address}");
        ws.Start();
        debug.Log($"Server started");
    }
}

/*
public class MyWebSocketServer : WebSocketServer.WebSocketServer
{
    override public void OnOpen(WebSocketConnection connection)
    {
        // Here, (string)connection.id gives you a unique ID to identify the client.
        Debug.Log(connection.id);
    }

    override public void OnMessage(WebSocketMessage message)
    {
        // (WebSocketConnection)message.connection gives you the connection that send the message.
        // (string)message.id gives you a unique ID for the message.
        // (string)message.data gives you the message content.
        Debug.Log(message.connection.id);
        Debug.Log(message.id);
        Debug.Log(message.data);
    }

    override public void OnClose(WebSocketConnection connection)
    {
        // Here is the same as OnOpen
        Debug.Log(connection.id);
    }

    public void onConnectionOpened(WebSocketConnection connection)
    {
        Debug.Log("Connection opened: " + connection.id);
    }

    public void onMessageReceived(WebSocketMessage message)
    {
        Debug.Log("Received new message: " + message.data);
    }

    public void onConnectionClosed(WebSocketConnection connection)
    {
        Debug.Log("Connection closed: " + connection.id);
    }
} */
