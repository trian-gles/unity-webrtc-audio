using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class SimpleDataChannelService : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log("Server data channel started!");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log(ID + " - DataChannel SERVER got message " + e.Data);
        foreach (var id in Sessions.ActiveIDs)
        {   
            if (id != ID)
            {
                Sessions.SendTo(e.Data, id);
            }
        }
    }


}
