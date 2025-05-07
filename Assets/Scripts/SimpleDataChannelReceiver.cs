
 using UnityEngine;
using System;
using Unity.WebRTC;
using System.Collections;
using NativeWebSocket;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

public class SimpleDataChannelReceiver : MonoBehaviour
{
    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    private WebSocket ws;
    private string clientId;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

    private void Start()
    {
        InitClient("192.168.1.139", 8080);
    }

    private void Update()
    {
        if (hasReceivedOffer)
        {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }
        #if !UNITY_WEBGL || UNITY_EDITOR
            ws.DispatchMessageQueue();
        #endif
    }

    async void InitClient(string serverIp, int serverPort)
    {
        int port = serverPort == 0 ? 8080 : serverPort;
        clientId = gameObject.name;

        ws = new WebSocket($"ws://{serverIp}:{port}/{nameof(SimpleDataChannelService)}");

        ws.OnOpen += () =>
        {
            Debug.Log("Headset connection open!");
        };

        ws.OnMessage += (bytes) =>
        {
            var e = System.Text.Encoding.UTF8.GetString(bytes);
            var requestArray = e.Split("!");
            var requestType = requestArray[0];
            var requestData = requestArray[1];

            switch (requestType) {
                case "OFFER":
                    Debug.Log(clientId + " - Got OFFER from Desktop: " + requestData); 
                    receivedOfferSessionDescTemp = SessionDescription.FromJSON(requestData);
                    hasReceivedOffer = true;
                    break;
                case "CANDIDATE":
                    Debug.Log(clientId + " - Got CANDIDATE from Desktop: " + requestData);
                    var candidateInit = CandidateInit.FromJSON(requestData);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();

                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;
                    var candidate = new RTCIceCandidate(init);

                    connection.AddIceCandidate(candidate);
                    break;
                default:
                    Debug.Log(clientId + "Desktop says: " + e);
                    break;
            }
        };

        

        connection = new RTCPeerConnection();
        connection.OnIceCandidate = candidate => {
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
            Debug.Log(state);
        };

        connection.OnDataChannel = channel => {
            dataChannel = channel;
            dataChannel.OnMessage = bytes =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("Message received: " + message);
            };

        };

        var receivedAudioSource = GetComponent<AudioSource>();
        var receiveStream = new MediaStream();
        receiveStream.OnAddTrack = e =>
        {
            if (e.Track is AudioStreamTrack track)
            {
                receivedAudioSource.SetTrack(track);

                receivedAudioSource.loop = true;
                receivedAudioSource.Play();
            }
        };

        connection.OnTrack = (RTCTrackEvent e) =>
        {
            Debug.Log("Listener added track");
            if (e.Track.Kind == TrackKind.Audio)
            {
                receiveStream.AddTrack(e.Track);
            }
        };

        await ws.Connect();
    }

    private IEnumerator CreateAnswer()
    {
        RTCSessionDescription offerSessionDesc = new RTCSessionDescription();
        offerSessionDesc.type = RTCSdpType.Offer;
        offerSessionDesc.sdp = receivedOfferSessionDescTemp.Sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref offerSessionDesc);
        yield return remoteDescOp;

        var answer = connection.CreateAnswer();
        yield return answer;

        var answerDesc = answer.Desc;
        var localDescOp = connection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        var answerSessionDesc = new SessionDescription()
        {
            SessionType = answerDesc.type.ToString(),
            Sdp = answerDesc.sdp
        };

        ws.SendText("ANSWER!" + answerSessionDesc.ConvertToJSON());

    }

    private void OnDestroy()
    {
        dataChannel.Close();
        connection.Close();
        ws.Close();
    }
}