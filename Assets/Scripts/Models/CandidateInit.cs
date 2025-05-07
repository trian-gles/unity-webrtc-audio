using UnityEngine;
using System;
using System.Runtime.InteropServices.WindowsRuntime;

public class CandidateInit : IJsonObject<CandidateInit>
{
    public string Candidate;
    public string SdpMid;
    public int SdpMLineIndex;

    public static CandidateInit FromJSON(string jsonString)
    {
        return JsonUtility.FromJson<CandidateInit>(jsonString);
    }

    public string ConvertToJSON() 
    { 
        return JsonUtility.ToJson(this);
    }
}
