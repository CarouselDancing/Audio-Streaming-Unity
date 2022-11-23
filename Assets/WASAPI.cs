using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;

[RequireComponent(typeof(AudioSource))]
public class WASAPI : MonoBehaviour
{
    // Native plugin functions
    const string dll = "mic_reader";
    [DllImport(dll)]
    private static extern void Init(); // Initializes plugin functionality
    [DllImport(dll)]
    private static extern void Shutdown(); // Shuts down plugin functionality
/*    [DllImport(dll)]
    private static extern void StartThreads(); // Starts threads for capture and data sending/receiving*/
    [DllImport(dll)]
    private static extern IntPtr GetPacket(out int numBytes, out int numChannels, out int outSampleRate); // Gets audio bytes received from network


    // Public variables
    public int SelectedDevice;
    public float[] publicSamples;
    // Private variables

    //public int packetSize = 20480, maxPackets = 10;
    private AudioSource audioSource;
    private AudioClip audioClip;
    private bool initialized = false;
    private bool paramsSelected = false;
    public int channels = -1, sampleRate = -1;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        Init();
        initialized = true;
/*        if ()
        {
            initialized = true;
            Debug.Log("Init success");
            //StartThreads();
            //Debug.Log("Threads started");
        }
        else
        {
            Debug.Log("Init failed");
        }*/
    }

    private void FixedUpdate()
    {
        if (initialized)
        {
            //bool packetAvailable = false;
            byte[] packet = ReceivedBytes();

            if (packet.Length > 0) // If packets are available made audio clip
            {
                //Debug.Log("Packet Read");
                // Debug.Log(packet.Length + " Bytes");
                ClipFromPacket(packet);
            }
/*            else
            {
                Debug.Log("No Packet");
            }*/
        }
    }

    private void OnApplicationQuit()
    {
        if (initialized)
        {
            Shutdown();
        }
    }

    public void ResetPlugin()
    {
        if (initialized)
        {
            Shutdown();
        }

        Init();
        initialized = true;
    }

    private byte[] ReceivedBytes() // Get received audio packets from native plugin
    {
        int numBytes;
        IntPtr receivedBytes = GetPacket(out numBytes, out channels, out sampleRate);
/*        if (!paramsSelected)
        {*/
            //sampleRate = IndexToSampleRate(sampleRateIndex);
/*            paramsSelected = true;
        }*/

        //Debug.Log("Channels: " + channels + ", Sample rate index: " + sampleRate);
        //Debug.Log("Num packets: " + numPackets);
        byte[] bytes = new byte[numBytes];
        Marshal.Copy(receivedBytes, bytes, 0, numBytes);
        return bytes;
    }

    private void ClipFromPacket(byte[] inPackets) // Create an audio clip from the received data packets and play the audio clip
    {
        float[] samples = new float[inPackets.Length / 4];

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = BitConverter.ToSingle(inPackets, i * 4);
        }
        publicSamples = samples;
        audioClip = AudioClip.Create("ReceivedAudio", samples.Length, channels, sampleRate, false);
        audioClip.SetData(samples, 0);
       // Debug.Log("Sample rate: " + sampleRate + ", Channels: " + channels + ", Samples: " + samples.Length); 

        audioSource.PlayOneShot(audioClip);
    }
}

/*// Custom editor functionality for audio plugin
[CustomEditor(typeof(WASAPI))]
public class DeviceSelect : Editor
{
    WASAPI mainScript;
    string[] devices;

    private void OnEnable()
    {
        devices = Microphone.devices;
        Array.Sort(devices);
        mainScript = (WASAPI)target;
    }
    public override void OnInspectorGUI()
    {
        //EditorGUILayout.Separator();
        //mainScript.SelectedDevice = EditorGUILayout.Popup(new GUIContent("Input Device", "Selects the desired input device for audio capture"), mainScript.SelectedDevice, devices);
        EditorGUILayout.Separator();
        mainScript.packetSize = EditorGUILayout.IntField(new GUIContent("Packet Size", "Selects the desired packet size"), mainScript.packetSize);
        mainScript.maxPackets = EditorGUILayout.IntField(new GUIContent("Max Packets", "Selects the maximum number of packets to be pulled into Unity between Update calls"), mainScript.maxPackets);
        EditorGUILayout.Separator();
        if (GUILayout.Button(new GUIContent("Reset Plugin", "Resets the plugin, applying the above parameters")))
        {
            mainScript.ResetPlugin();
        }
    }
}*/