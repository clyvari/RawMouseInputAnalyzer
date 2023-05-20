// Unity PINVOKE interface for pastebin.com/0Szi8ga6 
// Handles multiple cursors
// License: CC0

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class MouseInputManager : MonoBehaviour
{
    public static MouseInputManager instance;

    [DllImport("LibRawInput")]
    private static extern bool init();

    [DllImport("LibRawInput")]
    private static extern bool kill();

    [DllImport("LibRawInput")]
    private static extern IntPtr poll();

    public const byte RE_DEVICE_CONNECT = 0;
    public const byte RE_MOUSE = 2;
    public const byte RE_DEVICE_DISCONNECT = 1;
    public string getEventName(byte id)
    {
        switch (id)
        {
            case RE_DEVICE_CONNECT: return "RE_DEVICE_CONNECT";
            case RE_DEVICE_DISCONNECT: return "RE_DEVICE_DISCONNECT";
            case RE_MOUSE: return "RE_MOUSE";
        }
        return "UNKNOWN(" + id + ")";
    }

    public GameObject cursor;
    public Color[] colors;
    public float defaultMiceSensitivity = 1f;
    public float accelerationThreshold = 40;
    public float accelerationMultiplier = 2;
    public int screenBorderPixels = 16;

    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputEvent
    {
        public int devHandle;
        public int x, y, wheel;
        public byte press;
        public byte release;
        public byte type;
    }

    public class MousePointer
    {
        public GameObject obj;
        public Vector2 position;
        public int deviceID;
        public int playerID;
        public float sensitivity;
    }
    Dictionary<int, MousePointer> pointersByDeviceId = new Dictionary<int, MousePointer>();
    Dictionary<int, MousePointer> pointersByPlayerId = new Dictionary<int, MousePointer>();
    int nextPlayerId = 1;
    int miceCount = 0;

    Canvas canvas;
    RectTransform canvasRect;
    float width, height;

    void Start()
    {
        instance = this;
        bool res = init();
        Debug.Log("Init() ==> " + res);
        Debug.Log(Marshal.SizeOf(typeof(RawInputEvent)));
        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();
        //enterSingleMode();
    }

    public void OnDestroy()
    {
        instance = null;
    }

    int addCursor(int deviceId)
    {
        if(!isInit)
        {
            Debug.LogError("Not initialized");
            return -1;
        }

        MousePointer mp = null;
        pointersByDeviceId.TryGetValue(deviceId, out mp);
        if(mp != null)
        {
            Debug.LogError("This device already has a cursor");
            return -1;
        }

        Debug.Log("Adding DeviceID " + deviceId);
        mp = new MousePointer();
        mp.playerID = nextPlayerId++;
        pointersByDeviceId[deviceId] = mp;
        pointersByPlayerId[mp.playerID] = mp;
        mp.position = new Vector3(width / 2, height / 2, 0);
        
        mp.obj = Instantiate(cursor, transform) as GameObject;
        var rt = mp.obj.GetComponent<RectTransform>();
        rt.position = mp.position;

        var spriteComp = mp.obj.GetComponent<Image>();
        if (spriteComp) spriteComp.color = colors[mp.playerID % colors.Length];

        ++miceCount;
        return mp.playerID;
    }

    void deleteCursor(int deviceId)
    {
        --miceCount;
        var mp = pointersByDeviceId[deviceId];
        pointersByDeviceId.Remove(mp.deviceID);
        pointersByPlayerId.Remove(mp.playerID);
        Destroy(mp.obj);
    }

    bool _isMultiplayer = true;
    MousePointer _spPointer;

    [SerializeField]
    public bool isMultiplayer
    {
        set
        {
            if (!value) enterSingleMode(); else enterMultipleMode();
            _isMultiplayer = value;
        }
        get { return _isMultiplayer; }
    }

    void enterSingleMode()
    {
        clearCursorsAndDevices();
        --nextPlayerId;
        addCursor(0);
        _spPointer = pointersByDeviceId[0];
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }

    void enterMultipleMode()
    {
        _spPointer = null;
        nextPlayerId = 0;
        clearCursorsAndDevices();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void clearCursorsAndDevices()
    {
        pointersByDeviceId.Clear();
        pointersByPlayerId.Clear();
        nextPlayerId = 1;
        miceCount = 0;
        foreach (Transform t in transform) Destroy(t.gameObject);
    }

    public MousePointer getByPlayerId(int id)
    {
        MousePointer res = null;
        pointersByPlayerId.TryGetValue(id, out res);
        return res;
    }

    // Update is called once per frame
    int lastEvents = 0;
    bool isInit = true;
    void Update()
    {
        // Keyboard controls debug
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (isInit)
            {
                clearCursorsAndDevices();
                kill();
                isInit = false;
            }
            else
            {
                init();
                isInit = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            isMultiplayer = !isMultiplayer;
        }

        // SP
        if (!_isMultiplayer)
        {
            var rt = _spPointer.obj.GetComponent<RectTransform>();
            rt.position = Input.mousePosition;
        }
        else
        {
            // MP
            width = canvasRect.rect.width;
            height = canvasRect.rect.height;
            var left = -width / 2;
            var right = width / 2;
            var top = -height / 2;
            var bottom = height / 2;
            //Debug.Log("left=" + left + ", right=" + right + ", top=" + top + ", bottom=" + bottom);

            // Poll the events and properly update whatever we need
            IntPtr data = poll();
            int numEvents = Marshal.ReadInt32(data);
            if (numEvents > 0) lastEvents = numEvents;
            for (int i = 0; i < numEvents; ++i)
            {
                var ev = new RawInputEvent();
                long offset = data.ToInt64() + sizeof(int) + i * Marshal.SizeOf(ev);
                ev.devHandle = Marshal.ReadInt32(new IntPtr(offset + 0));
                ev.x = Marshal.ReadInt32(new IntPtr(offset + 4));
                ev.y = Marshal.ReadInt32(new IntPtr(offset + 8));
                ev.wheel = Marshal.ReadInt32(new IntPtr(offset + 12));
                ev.press = Marshal.ReadByte(new IntPtr(offset + 16));
                ev.release = Marshal.ReadByte(new IntPtr(offset + 17));
                ev.type = Marshal.ReadByte(new IntPtr(offset + 18));
                //Debug.Log(getEventName(ev.type) + ":  H=" + ev.devHandle + ";  (" + ev.x + ";" + ev.y + ")  Down=" + ev.press + " Up=" + ev.release);

                if (ev.type == RE_DEVICE_CONNECT) addCursor(ev.devHandle);
                else if (ev.type == RE_DEVICE_DISCONNECT) deleteCursor(ev.devHandle);
                else if (ev.type == RE_MOUSE)
                {
                    MousePointer pointer = null;
                    if (pointersByDeviceId.TryGetValue(ev.devHandle, out pointer))
                    {
                        float dx = ev.x * defaultMiceSensitivity;
                        float dy = ev.y * defaultMiceSensitivity;
                        if (Mathf.Abs(dx) > accelerationThreshold) dx *= accelerationMultiplier;
                        if (Mathf.Abs(dy) > accelerationThreshold) dy *= accelerationMultiplier;
                        pointer.position = new Vector2(
                            Mathf.Clamp(pointer.position.x + dx, screenBorderPixels, width - screenBorderPixels),
                            Mathf.Clamp(pointer.position.y - dy, screenBorderPixels, height - screenBorderPixels));
                        RectTransform rt = pointer.obj.GetComponent<RectTransform>();
                        rt.position = pointer.position;
                    }
                    else
                    {
                        Debug.Log("Unknown device found");
                        addCursor(ev.devHandle);
                    }
                }
            }
            Marshal.FreeCoTaskMem(data);
        }
    }

    void OnApplicationQuit()
    {
        kill();
    }
}