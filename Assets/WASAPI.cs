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
    private static extern int Init(); // Initializes plugin functionality
    [DllImport(dll)]
    private static extern void Shutdown(); // Shuts down plugin functionality

/*    [DllImport(dll)]
    private static extern IntPtr GetPacket(out int numChannels, out int outSampleRate); // Gets audio bytes received from network*/

    [DllImport(dll)]
    private static extern IntPtr GetPacket(out bool packetAvailable, out int numChannels, out int outSampleRate, out UInt64 time); // Gets audio bytes received from network

    [DllImport(dll)]
    private static extern void StartThread(); // Starts read thread and passes delegate for callback

    [System.Serializable]
    public class AudioBuffer
    {
        public AudioSource audioSource;
        public List<float> audioBuffer;
        public Coroutine coroutine;
        public int lastPacketNum = 0;
        public int numIndices = 5;
        public int prevTime = -1;
        public int writeIndex = 0;
        public bool clipInit = false;
        public void ResetBuffer()
        {
            audioSource.clip.SetData(new float[audioSource.clip.samples], 0);
            audioSource.Stop();
        }
    }
    public AudioBuffer streamedAudio;

    private bool paramsSelected = false, bufferInitialized = false;
    //private int sampleRate, numChannels;
    private const int bufferSize = 960000;

    private int packetSize;
    float[] packet;

    void Start()
    {
        packetSize = Init();
        packet = new float[packetSize];

        StartThread();
        InitBuffer(2, 48000);
    }

    void InitBuffer(int numChannels, int sampleRate)
    {
        bufferInitialized = true;
        streamedAudio = new AudioBuffer();
        streamedAudio.audioSource = GetComponent<AudioSource>();
        streamedAudio.audioBuffer = new List<float>(bufferSize);
        streamedAudio.audioSource.clip = AudioClip.Create("StreamedAudio", 4096, numChannels, sampleRate, true, ReadAudio);
        //streamedAudio.coroutine = StartCoroutine(UpdateAudioSource());
        streamedAudio.audioSource.loop = true;
        streamedAudio.audioSource.Play();
    }

    void ReadAudio(float[] data)
    {
        int dataRemaining = data.Length;
        if (streamedAudio.audioBuffer.Count > 0) // Copy data from the buffer before pulling new data from native
        {
            int toCopy = (data.Length > streamedAudio.audioBuffer.Count) ? streamedAudio.audioBuffer.Count : data.Length;
            Array.Copy(streamedAudio.audioBuffer.ToArray(), data, toCopy);
            streamedAudio.audioBuffer.RemoveRange(0, toCopy);
            dataRemaining -= toCopy;
        }

        if (dataRemaining > 0) // If more data is needed pull from native and add excess to the buffer
        {
            bool packetAvailable = false;
            int numChannels, sampleRate;
            UInt64 packetTime;
            IntPtr receivedSamples = GetPacket(out packetAvailable, out numChannels, out sampleRate, out packetTime);
            if (packetAvailable)
            {
                Marshal.Copy(receivedSamples, packet, 0, packetSize);
                ulong latency = TimeSinceEpoch() - packetTime;
                Debug.Log(latency + "ms");
                int packetRemaining = packetSize;
                int toCopy = (packetSize > dataRemaining) ? dataRemaining : packetSize;

                Array.Copy(packet, 0, data, data.Length - dataRemaining, toCopy);
                packetRemaining -= toCopy;

                if (packetRemaining > 0) // Move the rest of the packet to the buffer
                {
                    float[] remainingPacket = new float[packetRemaining];
                    Array.Copy(packet, packet.Length - packetRemaining, remainingPacket, 0, packetRemaining);
                    streamedAudio.audioBuffer.AddRange(remainingPacket);
                }
            }
        }
    }

    ulong TimeSinceEpoch()
    {
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        return (ulong)(System.DateTime.UtcNow - epochStart).TotalMilliseconds;
    }

    private void OnApplicationQuit()
    {
        Shutdown();
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