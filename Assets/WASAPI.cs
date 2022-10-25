using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.InteropServices;
public class WASAPI : MonoBehaviour
{
    // Native plugin functions
    const string dll = "Audio-Client";
    [DllImport(dll)]
    private static extern bool Init(int size, int maxPacks); // Initializes plugin functionality
    [DllImport(dll)]
    private static extern bool Shutdown(); // Shuts down plugin functionality
    [DllImport(dll)]
    private static extern void StartThreads(); // Starts threads for capture and data sending/receiving
    [DllImport(dll)]
    private static extern IntPtr GetPacket(out bool packetAvailable, out int numPackets); // Gets audio bytes received from network


    // Public variables
    public AudioSource audioSource;
    public int maxPackets = 32, numberOfPackets = 0;
    public const int packetSize = 256;
    //byte[] receivedPacket = new byte[packetSize];
    //float[] samples = new float[packetSize / 4];
    public float maxSample = -15f, minSample = 15f;
    bool initialized = false;
    AudioClip audioClip;
    void Start()
    {
        if (Init(packetSize, maxPackets))
        {
            initialized = true;
            Debug.Log("Init success");
            StartThreads();
            Debug.Log("Threads started");
        }
        else
        {
            Debug.Log("Init failed");
        }
    }

    byte[] ReceivedBytes(out bool packetAvailable, out int numPackets) // Get received audio packets from native plugin
    {
        IntPtr receivedBytes = GetPacket(out packetAvailable, out numPackets);
        byte[] bytes = new byte[packetSize * numPackets];
        Marshal.Copy(receivedBytes, bytes, 0, packetSize * numPackets);
        return bytes;
    }

    void ClipFromPacket(byte[] inPackets) // Create an audio clip from the received data packets and play the audio clip
    {
        float[] samples = new float[inPackets.Length / 4];

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = BitConverter.ToSingle(inPackets, i * 4);
        }
        audioClip = AudioClip.Create("ReceivedAudio", samples.Length, 2, 44100, false);
        audioClip.SetData(samples, 0);
        //audioSource.clip = audioClip;
        audioSource.PlayOneShot(audioClip);
    }

    private void FixedUpdate()
    {
        if (initialized)
        {
            bool packetAvailable = false;
            byte[] packet = ReceivedBytes(out packetAvailable, out numberOfPackets);

            if (packetAvailable) // If packets are available made audio clip
            {
                ClipFromPacket(packet);
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (initialized)
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
}
