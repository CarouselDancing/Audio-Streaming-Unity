using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.InteropServices;
public class WASAPI : MonoBehaviour
{
    const string dll = "WASAPI-Audio";
    [DllImport(dll)]
    private static extern bool Init();
    [DllImport(dll)]
    private static extern bool Shutdown();
    [DllImport(dll)]
    private static extern void StartThreads();
    [DllImport(dll)]
    private static extern IntPtr GetCapturedBytes(out int outSize);
    [DllImport(dll)]
    private static extern IntPtr GetReceivedBytes(out int outSize);
    void Start()
    {
        if (Init())
        {
            Debug.Log("Init success");
            StartThreads();
            Debug.Log("Threads started");
        }
        else
        {
            Debug.Log("Init failed");
        }


    }

    byte[] CapturedBytes()
    {
        int size = 0;
        IntPtr capturedBytes = GetCapturedBytes(out size);
        byte[] bytes = new byte[size];
        Marshal.Copy(capturedBytes, bytes, 0, size);
        return bytes;
    }

    byte[] ReceivedBytes()
    {
        int size = 0;
        IntPtr receivedBytes = GetReceivedBytes(out size);
        byte[] bytes = new byte[size];
        Marshal.Copy(receivedBytes, bytes, 0, size);
        return bytes;
    }

    public int numRecBytes = 0, numReceived = 0;
    public int numCapBytes = 0, numCaptured = 0;

    private void Update()
    {
        byte[] capturedBytes = CapturedBytes();
        byte[] receivedBytes = ReceivedBytes();
        if (capturedBytes.Length > 0)
        {
            numCapBytes = capturedBytes.Length;
            numCaptured++;
        }
        if (receivedBytes.Length > 0)
        {
            numRecBytes = receivedBytes.Length;
            numReceived++;
        }
    }

    private void OnApplicationQuit()
    {
        if (Shutdown())
        {
            Debug.Log("Shutdown success");
        }
        else
        {
            Debug.Log("Shutdown failed");
        }
    }
}
