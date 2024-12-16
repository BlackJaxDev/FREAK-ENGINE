using System.Net.Sockets;
using System.Net;
using System.Numerics;
using XREngine.Components;
using System.Text;

namespace XREngine.Scene.Components;

/// <summary>
/// Receives and processes VMC motion capture information.
/// </summary>
public class VMCCaptureComponent : XRComponent
{
    private const int Port = 39539; // Common VMC Protocol port
    private const string CMD_BoneTransform = "/VMC/Ext/Bone/Pos";
    private const string CMD_BlendshapeValue = "/VMC/Ext/Blend/Val";
    private const string CMD_BlendshapeApply = "/VMC/Ext/Blend/Apply";
    private const string CMD_RootTransform = "/VMC/Ext/Root/Pos";
    private const string CMD_CameraTransform = "/VMC/Ext/Cam";
    private const string CMD_LightTransform = "/VMC/Ext/Light";
    private const string CMD_CalibrationReady = "/VMC/Ext/Set/Calib/Ready";
    private const string CMD_CalibrationExecute = "/VMC/Ext/Set/Calib/Exec";
    private const string CMD_KeyboardInput = "/VMC/Ext/Key";
    private const string CMD_MidiNote = "/VMC/Ext/Midi/Note";
    private const string CMD_MidiCC = "/VMC/Ext/Midi/CC/Val";
    private const string CMD_VRMInformation = "/VMC/Ext/VRM";
    private UdpClient? _udpClient;

    private Dictionary<string, Vector3> bonePositions = new Dictionary<string, Vector3>();
    private Dictionary<string, Quaternion> boneRotations = new Dictionary<string, Quaternion>();
    private Dictionary<string, float> blendShapes = new Dictionary<string, float>();
    private Dictionary<string, Vector3> trackerPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, Quaternion> trackerRotations = new Dictionary<string, Quaternion>();
    private Dictionary<string, string> configurationPaths = new Dictionary<string, string>();

    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private float cameraFov;

    private Vector4 lightColor;
    private Vector3 lightPosition;
    private Quaternion lightRotation;

    protected internal override void OnComponentActivated()
    {
        base.OnComponentActivated();
        _udpClient = new UdpClient(Port);
        Engine.Time.Timer.CollectVisible += ReceiveData;
        RegisterTick(ETickGroup.Normal, ETickOrder.Animation, UpdateModel);
    }
    protected internal override void OnComponentDeactivated()
    {
        var client = _udpClient;
        _udpClient = null;
        client?.Close();
        Engine.Time.Timer.CollectVisible -= ReceiveData;
        base.OnComponentDeactivated();
    }

    private void ReceiveData()
    {
        IPEndPoint endPoint = new(IPAddress.Any, Port);
        try
        {
            while (_udpClient is not null)
            {
                byte[] data = _udpClient.Receive(ref endPoint);
                string message = Encoding.UTF8.GetString(data);
                ParseMessage(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving data: {ex.Message}");
        }
    }

    private void ParseMessage(string message)
    {
        string[] lines = message.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(' ');
            switch (parts[0])
            {
                case CMD_BoneTransform:
                    ParseBonePosition(parts);
                    break;
                case CMD_BlendshapeValue:
                    ParseBlendShape(parts);
                    break;
                case CMD_BlendshapeApply:
                    ApplyBlendShapes();
                    break;
                case CMD_RootTransform:
                    ParseRootPosition(parts);
                    break;
                case CMD_CameraTransform:
                    ParseCamera(parts);
                    break;
                case CMD_LightTransform:
                    ParseLight(parts);
                    break;
                case CMD_CalibrationReady:
                    Console.WriteLine("Calibration ready requested.");
                    break;
                case CMD_CalibrationExecute:
                    ExecuteCalibration(parts);
                    break;
                case CMD_KeyboardInput:
                    ParseKeyboardInput(parts);
                    break;
                case CMD_MidiNote:
                    ParseMidiNoteInput(parts);
                    break;
                case CMD_MidiCC:
                    ParseMidiCCValue(parts);
                    break;
                case CMD_VRMInformation:
                    ParseVRMInfo(parts);
                    break;
                default:
                    Console.WriteLine($"Unknown command: {line}");
                    break;
            }
        }
    }

    private void ParseBonePosition(string[] parts)
    {
        if (parts.Length != 9)
            return;

        string boneName = parts[1];

        Vector3 position = new(
            float.Parse(parts[2]),
            float.Parse(parts[3]),
            float.Parse(parts[4]));

        Quaternion rotation = new(
            float.Parse(parts[5]),
            float.Parse(parts[6]),
            float.Parse(parts[7]),
            float.Parse(parts[8]));

        lock (bonePositions)
            bonePositions[boneName] = position;

        lock (boneRotations)
            boneRotations[boneName] = rotation;
    }

    /// <summary>
    /// Sets a blend shape value to be applied.
    /// </summary>
    /// <param name="parts"></param>
    private void ParseBlendShape(string[] parts)
    {
        if (parts.Length != 3)
            return;

        string blendShapeName = parts[1];
        float value = float.Parse(parts[2]);

        lock (blendShapes)
            blendShapes[blendShapeName] = value;
    }

    /// <summary>
    /// Queues an application of all modified blend shapes.
    /// </summary>
    private void ApplyBlendShapes()
    {
        lock (blendShapes)
        {
            Console.WriteLine("Applying blend shapes...");
            foreach (var kvp in blendShapes)
                Console.WriteLine($"BlendShape: {kvp.Key}, Value: {kvp.Value}");
        }
    }

    private void ParseRootPosition(string[] parts)
    {
        if (parts.Length < 8)
            return;

        string rootName = parts[1];

        Vector3 position = new(
            float.Parse(parts[2]),
            float.Parse(parts[3]),
            float.Parse(parts[4]));

        Quaternion rotation = new(
            float.Parse(parts[5]),
            float.Parse(parts[6]),
            float.Parse(parts[7]),
            float.Parse(parts[8]));

        Console.WriteLine($"Root {rootName}: Position={position}, Rotation={rotation}");
    }

    private void ParseCamera(string[] parts)
    {
        if (parts.Length != 10)
            return;

        cameraPosition = new Vector3(
            float.Parse(parts[2]),
            float.Parse(parts[3]),
            float.Parse(parts[4]));

        cameraRotation = new Quaternion(
            float.Parse(parts[5]),
            float.Parse(parts[6]),
            float.Parse(parts[7]),
            float.Parse(parts[8]));

        cameraFov = float.Parse(parts[9]);

        Console.WriteLine($"Camera Position={cameraPosition}, Rotation={cameraRotation}, FOV={cameraFov}");
    }

    private void ParseLight(string[] parts)
    {
        if (parts.Length != 13)
            return;

        lightPosition = new Vector3(
            float.Parse(parts[2]),
            float.Parse(parts[3]),
            float.Parse(parts[4]));

        lightRotation = new Quaternion(
            float.Parse(parts[5]),
            float.Parse(parts[6]),
            float.Parse(parts[7]),
            float.Parse(parts[8]));

        lightColor = new Vector4(
            float.Parse(parts[9]),
            float.Parse(parts[10]),
            float.Parse(parts[11]),
            float.Parse(parts[12]));

        Console.WriteLine($"Light Position={lightPosition}, Rotation={lightRotation}, Color={lightColor}");
    }

    private void ExecuteCalibration(string[] parts)
    {
        if (parts.Length != 2)
            return;

        int mode = int.Parse(parts[1]);
        Console.WriteLine($"Executing calibration with mode: {mode}");
    }

    private void ParseKeyboardInput(string[] parts)
    {
        if (parts.Length != 4)
            return;

        int active = int.Parse(parts[1]);
        string keyName = parts[2];
        int keyCode = int.Parse(parts[3]);

        Console.WriteLine($"Keyboard Input: {keyName} (KeyCode={keyCode}), Active={active}");
    }

    private void ParseMidiNoteInput(string[] parts)
    {
        if (parts.Length != 5)
            return;

        int active = int.Parse(parts[1]);
        int channel = int.Parse(parts[2]);
        int note = int.Parse(parts[3]);
        float velocity = float.Parse(parts[4]);

        Console.WriteLine($"MIDI Note: Channel={channel}, Note={note}, Velocity={velocity}, Active={active}");
    }

    private void ParseMidiCCValue(string[] parts)
    {
        if (parts.Length != 3)
            return;

        int knob = int.Parse(parts[1]);
        float value = float.Parse(parts[2]);

        Console.WriteLine($"MIDI CC Value: Knob={knob}, Value={value}");
    }

    private void ParseVRMInfo(string[] parts)
    {
        if (parts.Length < 3)
            return;

        string path = parts[1];
        string title = parts[2];
        Console.WriteLine($"Loaded VRM: Path={path}, Title={title}");
    }

    private void UpdateModel()
    {

    }
}