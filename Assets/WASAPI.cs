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

    [DllImport(dll)]
    private static extern IntPtr GetPacket(out int numSamples, out int numChannels, out int outSampleRate); // Gets audio bytes received from network

    [DllImport(dll)]
    private static extern void StartThread(myCallbackDelegate cb); // Starts read thread and passes delegate for callback

    [System.Serializable]
    public class AudioBuffer
    {
        public AudioSource audioSource;
        public List<float> audioBuffer;
        public Coroutine coroutine;
        public int lastPacketNum = 0;
        public int numIndices = 2;
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
    private const int bufferSize = 100000;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void myCallbackDelegate(int a);

    myCallbackDelegate callbackDelegate;
    void Start()
    {
        callbackDelegate = new myCallbackDelegate(this.GetMicData);
        Init();
        StartThread(callbackDelegate);
        InitBuffer(2, 48000);
    }

    void InitBuffer(int numChannels, int sampleRate)
    {
        bufferInitialized = true;
        streamedAudio = new AudioBuffer();
        streamedAudio.audioSource = GetComponent<AudioSource>();
        streamedAudio.audioSource.clip = AudioClip.Create("StreamedAudio", sampleRate * streamedAudio.numIndices, numChannels, sampleRate, false);
        streamedAudio.audioBuffer = new List<float>(bufferSize);
        streamedAudio.coroutine = StartCoroutine(UpdateAudioSource());
        streamedAudio.audioSource.loop = true;
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }

/*    private void Update()
    {
        if (paramsSelected && !bufferInitialized)
        {
            InitBuffer();
        }
    }*/

    // Callback function that gets available audio data once it becomes available and adds it to the buffer
    void GetMicData(int packetNum)
    {
        if (streamedAudio.lastPacketNum >= packetNum)
        {
            return;
        }
        streamedAudio.lastPacketNum = packetNum;

        int numSamples, numChannels, sampleRate;
        IntPtr receivedBytes = GetPacket(out numSamples, out numChannels, out sampleRate);
        paramsSelected = true;

        if (numSamples > 0)
        {
            float[] samples = new float[numSamples];
            Marshal.Copy(receivedBytes, samples, 0, numSamples);

            if (numSamples + streamedAudio.audioBuffer.Count > streamedAudio.audioBuffer.Capacity)
            {
                streamedAudio.audioBuffer.RemoveRange(0, numSamples);
            }
            streamedAudio.audioBuffer.AddRange(samples);
        }
    }
    public int readIndex = 0, writeIndex = 0, samplesToWrite = 0;
    // Fills the audio clip with data from the buffer
    IEnumerator UpdateAudioSource()
    {
        bool updating = true;
        int prevTime = 0;
        while (updating)
        {

            if (streamedAudio.audioBuffer.Count > 0)
            {
                int endPoint = (streamedAudio.writeIndex < streamedAudio.audioSource.timeSamples) ? streamedAudio.audioSource.timeSamples : streamedAudio.audioSource.clip.samples;
                int toWrite = (streamedAudio.audioBuffer.Count < endPoint - streamedAudio.writeIndex) ? streamedAudio.audioBuffer.Count : endPoint - streamedAudio.writeIndex;
                if (toWrite % 2 != 0)
                {
                    toWrite--;
                }

                if (toWrite <= 0)
                {
                    continue;
                }

                Debug.Log(toWrite);
                int prevIndex = streamedAudio.writeIndex;
                streamedAudio.audioSource.clip.SetData(streamedAudio.audioBuffer.GetRange(0, toWrite - 1).ToArray(), streamedAudio.writeIndex);
                streamedAudio.audioBuffer.RemoveRange(0, toWrite - 1);


                streamedAudio.writeIndex = (streamedAudio.writeIndex + (int)(toWrite * 0.5f) >= streamedAudio.audioSource.clip.samples - 1) ? 0 : streamedAudio.writeIndex + (int)(toWrite * 0.5f);

                //Debug.Log("Wrote from " + prevIndex + " to " + streamedAudio.writeIndex + ", Played from " + prevTime + " to " + streamedAudio.audioSource.timeSamples);

                prevTime = streamedAudio.audioSource.timeSamples;

                if (!streamedAudio.audioSource.isPlaying)
                {
                    streamedAudio.audioSource.Play();
                }
                //yield return new WaitForSeconds(1f);
            }
            //Debug.Log(streamedAudio.audioSource.time + " of " + streamedAudio.audioSource.clip.length);
            //Debug.Log((streamedAudio.audioSource.clip.length));

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