using System.Collections;
using System.Runtime.CompilerServices;
using Unity.WebRTC;
using UnityEngine;
using NativeWebSocket;
using System.Net.Sockets;
using System.Net;

public class SimpleDataChannelSender : MonoBehaviour 
{
    [SerializeField]
    private bool sendMessageViaChannel = false;

    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;



    private WebSocket ws;
    private string clientId;

    private bool hasReceivedAnswer = false;
    private SessionDescription receivedAnswerSessionDescTemp;

    public Debugger debug;

    bool negotiationNeeded = false;
    bool headsetConnected = false;

    private void Start()
    {
        string serverIpv4Address = "";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                serverIpv4Address = ip.ToString();
                break;
            }
        }
        InitClient(serverIpv4Address, 8080);
        
    }

    private void OnDestroy()
    {
        dataChannel.Close();
        connection.Close();
    }

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
                ws.DispatchMessageQueue();
        #endif

        if (hasReceivedAnswer)
        {
            hasReceivedAnswer = !hasReceivedAnswer;
            StartCoroutine(SetRemoteDesc());

        }
        if (negotiationNeeded && headsetConnected)
        {
            negotiationNeeded = false;
            StartCoroutine(CreateOffer());
        }
        if (sendMessageViaChannel)
        {
            sendMessageViaChannel = !sendMessageViaChannel;
            dataChannel.Send("TEST!TEST!TEST");
        }
    }

    public async void InitClient(string serverIp, int serverPort)
    {
        int port = serverPort == 0? 8080 : serverPort;
        clientId = gameObject.name;

        ws = new WebSocket($"ws://{serverIp}:{port}/{nameof(SimpleDataChannelService)}");
        ws.OnMessage += (bytes) =>
        {
            var e = System.Text.Encoding.UTF8.GetString(bytes);
            var requestArray = e.Split("!");
            var requestType = requestArray[0];
            var requestData = requestArray[1];

            switch (requestType) 
            {
                case "ANSWER":
                    debug.Log(clientId + " - Got ANSWER from Headset: " + requestData);
                    receivedAnswerSessionDescTemp = SessionDescription.FromJSON(requestData);
                    hasReceivedAnswer = true;
                    break;
                case "CANDIDATE":
                    debug.Log(clientId + " - Got CANDIDATE from Headset: " + requestData);

                    var candidateInit = CandidateInit.FromJSON(requestData);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();
                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;
                    RTCIceCandidate candidate = new RTCIceCandidate(init);

                    connection.AddIceCandidate(candidate);
                    break;

                case "READY":
                    headsetConnected = true;
                    
                    break;
                default:
                    debug.Log(clientId + "Headset says: " + e);
                    break;

            }
        };

        ws.OnOpen += async () =>
        {
            await ws.SendText("Hello from the desktop!");
        };



        connection = new RTCPeerConnection();
        connection.OnIceCandidate = candidate =>
        {
            var candidateInit = new CandidateInit()
            {
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                Candidate = candidate.Candidate
            };
            ws.SendText("CANDIDATE!" + candidateInit.ConvertToJSON());
        };
        connection.OnIceConnectionChange = state =>
        {
            debug.Log(state.ToString());
        };




        dataChannel = connection.CreateDataChannel("sendChannel");
        dataChannel.OnOpen = () =>
        {
            debug.Log("Sender opened channel");
        };
        dataChannel.OnClose = () => {
            debug.Log("Sender closed channel");
        };

        connection.OnNegotiationNeeded = () =>
        {
            negotiationNeeded = true;
            
        };

        var inputAudioSource = GetComponent<AudioSource>();
        var track = new AudioStreamTrack(inputAudioSource);
        var sendStream = new MediaStream();
        var sender = connection.AddTrack(track, sendStream);


        await ws.Connect();
        
    }

    private void RTCInit()
    {
        
        
    }

    private IEnumerator CreateOffer() { 
    
        var offer = connection.CreateOffer();
        yield return offer;

        var offerDesc = offer.Desc;
        var localDescOp = connection.SetLocalDescription(ref offerDesc);
        yield return localDescOp;

        var offerSessionDesc = new SessionDescription()
        {
            SessionType = offerDesc.type.ToString(),
            Sdp = offerDesc.sdp
        };

        ws.SendText("OFFER!" + offerSessionDesc.ConvertToJSON());
    }

    private IEnumerator SetRemoteDesc() {
        var answerSessionDesc = new RTCSessionDescription();
        answerSessionDesc.type = RTCSdpType.Answer;
        answerSessionDesc.sdp = receivedAnswerSessionDescTemp.Sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref answerSessionDesc);
        yield return remoteDescOp;
    }
}
