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
    private static extern void Init(int inSize); // Initializes plugin functionality
    [DllImport(dll)]
    private static extern void Shutdown(); // Shuts down plugin functionality

    [DllImport(dll)]
    private static extern IntPtr GetPacket(out int numChannels, out int outSampleRate); // Gets audio bytes received from network

    [DllImport(dll)]
    private static extern void StartThread(myCallbackDelegate cb); // Starts read thread and passes delegate for callback

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
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void myCallbackDelegate(int a);

    myCallbackDelegate callbackDelegate;

    private const int packetSize = 96000;
    float[] packet;

    void Start()
    {
        callbackDelegate = new myCallbackDelegate(this.GetMicData);
        Init(packetSize);
        packet = new float[packetSize];

        StartThread(callbackDelegate);
        InitBuffer(2, 48000);
    }

    void InitBuffer(int numChannels, int sampleRate)
    {
        bufferInitialized = true;
        streamedAudio = new AudioBuffer();
        streamedAudio.audioSource = GetComponent<AudioSource>();
        streamedAudio.audioBuffer = new List<float>(bufferSize);
        streamedAudio.audioSource.clip = AudioClip.Create("StreamedAudio", 4096, numChannels, sampleRate, true, OnAudioRead);
        //streamedAudio.coroutine = StartCoroutine(UpdateAudioSource());
        streamedAudio.audioSource.loop = true;
        streamedAudio.audioSource.Play();
    }

    bool applying = false;
    void OnAudioRead(float[] data)
    {
        //Debug.Log(data.Length); 
        if (streamedAudio.audioBuffer.Count == 0 || applying)
        {
            return;
        }

        applying = true;
        int toWrite = (data.Length > streamedAudio.audioBuffer.Count) ? streamedAudio.audioBuffer.Count : data.Length;
        Debug.Log("ToWrite: " + toWrite + " Buffer: " + streamedAudio.audioBuffer.Count + " Data: "+ data.Length);
        for (int i = 0; i < toWrite; i++)
        {
            data[i] = streamedAudio.audioBuffer[i];
            //streamedAudio.audioBuffer.RemoveAt(0);
        }
        streamedAudio.audioBuffer.RemoveRange(0, toWrite);
        applying = false;
    }

    int TimeSinceEpoch()
    {
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        return (int)(System.DateTime.UtcNow - epochStart).TotalMilliseconds;
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }

    // Callback function that gets available audio data once it becomes available and adds it to the buffer
    void GetMicData(int packetNum)
    {
        if (streamedAudio.lastPacketNum > packetNum)
        {
            return;
        }
        streamedAudio.lastPacketNum = packetNum;

        int numChannels, sampleRate;
        IntPtr receivedBytes = GetPacket(out numChannels, out sampleRate);

        Marshal.Copy(receivedBytes, packet, 0, packetSize);

        if (packetSize + streamedAudio.audioBuffer.Count > streamedAudio.audioBuffer.Capacity)
        {
            streamedAudio.audioBuffer.RemoveRange(0, packetSize);
        }
        streamedAudio.audioBuffer.AddRange(packet);
        //Debug.Log(streamedAudio.audioBuffer.Count);
    }

    // Fills the audio clip with data from the buffer
    IEnumerator UpdateAudioSource()
    {
        bool updating = true;
        int prevTime = 0;
        while (updating)
        {

            if (streamedAudio.audioBuffer.Count > 0)
            {
                int endPoint = (streamedAudio.writeIndex < streamedAudio.audioSource.timeSamples * 2) ? streamedAudio.audioSource.timeSamples * 2 : streamedAudio.audioSource.clip.samples * 2;
                int toWrite = (streamedAudio.audioBuffer.Count < endPoint - streamedAudio.writeIndex) ? streamedAudio.audioBuffer.Count : endPoint - streamedAudio.writeIndex;

                if (toWrite % 2 != 0 || toWrite <= 0)
                {
                    continue;
                }


                Debug.Log(toWrite);
                int prevIndex = streamedAudio.writeIndex;
                streamedAudio.audioSource.clip.SetData(streamedAudio.audioBuffer.GetRange(0, toWrite).ToArray(), streamedAudio.writeIndex / 2);
                streamedAudio.audioBuffer.RemoveRange(0, toWrite);


                streamedAudio.writeIndex = (streamedAudio.writeIndex + toWrite >= (streamedAudio.audioSource.clip.samples - 1) * 2) ? 0 : streamedAudio.writeIndex + toWrite;

                //Debug.Log("Wrote from " + prevIndex + " to " + streamedAudio.writeIndex + ", Played from " + prevTime + " to " + streamedAudio.audioSource.timeSamples);

                prevTime = streamedAudio.audioSource.timeSamples;

                if (!streamedAudio.audioSource.isPlaying)
                {
                    streamedAudio.audioSource.Play();
                }
            }

            yield return null;
        }
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