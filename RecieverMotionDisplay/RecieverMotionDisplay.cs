using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System.Collections.Generic;
using System;

public class RecieverMotionDisplay : Mod
{
    private Harmony harmony;
    public static List<GameObject> speedDisplays = new List<GameObject>();

    public void Start()
    {
        harmony = new Harmony("com.aidanamite.RecieverMotionDisplay");
        harmony.PatchAll();
        Debug.Log("Mod RecieverMotionDisplay has been loaded!");
    }

    public void OnModUnload()
    {
        harmony.UnpatchAll();
        Debug.Log("Mod RecieverMotionDisplay has been unloaded!");
    }
}

[HarmonyPatch(typeof(Reciever), "HandleUI", MethodType.Normal)]
public class Patch_UIHandler
{
    static void Postfix(ref Reciever __instance)
    {
        Traverse recieverTracer = Traverse.Create(__instance);
        GameObject radarScreen = recieverTracer.Field("radarSection").GetValue<GameObject>();
        if (!__instance.battery.BatterySlotIsEmpty && __instance.battery.CanGiveElectricity && __instance.battery.On && __instance.HasCorrectAltitude && radarScreen.activeSelf)
        {
            UILine line = __instance.GetComponentInChildren<UILine>();
            float radarUIWidth = recieverTracer.Field("radarUIWidth").GetValue<float>();
            Raft theRaft = ComponentManager<Raft>.Value;
            if (theRaft == null)
                return;
            Vector3 movement = theRaft.Velocity;
            float speed = movement.DistanceXZ(Vector3.zero);
            foreach (Text gO in __instance.GetComponentsInChildren<Text>())
            {
                if (gO.name == "MovementText")
                {
                    gO.GetComponent<Text>().text = "Speed: " + RoundToSignificantDigits(speed,3) + "m/s"; // change the speed number display based on current speed
                    break;
                }
            }
            Network_Player player = RAPI.GetLocalPlayer();
            if (player == null)
                return;
            line.angle = (float)(__instance.transform.rotation.eulerAngles.y + Math.Asin(movement.x / speed) / Math.PI * 180 * (movement.z < 0 ? 1 : -1) + (movement.z < 0 ? 90 : -90)); // set angle of line to the relative angle of the raft's motion compared to the angle of the reciever
            line.length = Mathf.Min(speed * 20,radarUIWidth / 2f); // change line length based on current speed
            line.width = Mathf.Max(Mathf.Min(2 * Vector3.Distance(player.CameraTransform.position, __instance.transform.position), 10),2); // changes line width based on how far the player is from the screen to help visability
            line.color = Color.HSVToRGB(0.2f * speed / theRaft.maxSpeed, 1, 1); // change color based of speed percentage of max speed
        }
    }
    static string RoundToSignificantDigits(float d, int digits)
    {
        if (d == 0)
            return "0";

        float scale = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(Mathf.Abs(d))) - digits + 1);
        d = scale * Mathf.Round(d / scale);
        string num = Mathf.Abs(d).ToString().ToLower();
        string suf = "";
        if (num.Contains("e")) {
            suf = num.Substring(num.IndexOf("e"));
            num = num.Substring(0, num.Length - suf.Length);
        }
        int ind = num.IndexOf(".");
        string tmp = "";
        if (ind == -1)
        {
            tmp = num.Substring(0, Mathf.Min(digits, num.Length));
        } else
        {
            tmp = num.Substring(0, Mathf.Min(digits + (ind < digits ? 1 : 0), num.Length));
        }
        string tmp2 = num.Substring(tmp.Length);
        tmp2 = tmp2.Replace("1", "0").Replace("2", "0").Replace("3", "0").Replace("4", "0").Replace("5", "0").Replace("6", "0").Replace("7", "0").Replace("8", "0").Replace("9", "0");
        if (ind != -1)
            tmp2 = tmp2.TrimEnd('0').Replace(".","");
        return tmp + tmp2 + suf;
    }
}

[HarmonyPatch(typeof(Reciever), "Start", MethodType.Normal)]
public class Patch_RecieverInit
{
    static void Postfix(ref Reciever __instance)
    {
        float radarScreenSize = Traverse.Create(__instance).Field("radarUIWidth").GetValue<float>();
        Canvas canvas = __instance.GetComponentInChildren<Canvas>(true);
        Text anyText = canvas.GetComponentInChildren<Text>(true);
        GameObject text = CreateText(canvas.transform, 30, -30, "Speed: 1", 20, Color.white, 800, 600, anyText.font, "MovementText");
        GameObject speedLine = CreateLine(canvas,10,2,-13.9f, -3.1f, Color.red,-90f);
        Debug.Log("[Reciever Motion Display]: Patched Reciever");
        
    }

    static GameObject CreateText(Transform canvas_transform, float x, float y, string text_to_print, int font_size, Color text_color, float width, float height, Font font,string name = "Text")
    {
        GameObject UItextGO = new GameObject("Text");
        UItextGO.transform.SetParent(canvas_transform, false);
        RectTransform trans = UItextGO.AddComponent<RectTransform>();
        trans.sizeDelta = new Vector2(width,height);
        trans.anchoredPosition = new Vector2(x, y);
        Text text = UItextGO.AddComponent<Text>();
        text.text = text_to_print;
        text.font = font;
        text.fontSize = font_size;
        text.color = text_color;
        text.name = name;
        return UItextGO;
    }

    static GameObject CreateLine(Canvas canvas,float length,float width,float x,float y,Color lineColor,float angle)
    {
        GameObject lineContainer = new GameObject("Line");
        lineContainer.transform.SetParent(canvas.transform, false);
        RectTransform trans = lineContainer.AddComponent<RectTransform>();//new Vector2(length, width);
        trans.sizeDelta = canvas.pixelRect.size;
        trans.anchoredPosition = new Vector2(0, 0);
        UILine.setCreationValues(new Vector2(x, y), length, width, angle);
        UILine lineObj = lineContainer.AddComponent<UILine>();
        lineObj.color = lineColor;
        return lineContainer;
    }
}

public class UILine : Image
{
    private Vector2 _p;
    private Vector2 _s;
    private float _a;
    private static Vector2 _cP;
    private static Vector2 _cS;
    private static float _cA;
    public Vector2 start
    {
        get
        {
            return _p;
        }
        set
        {
            _p = value;
            UpdatePosition();
        }
    }
    public float length
    {
        get
        {
            return _s.x;
        }
        set
        {
            _s.x = value;
            UpdatePosition();
        }
    }
    public float angle
    {
        get
        {
            return _a;
        }
        set
        {
            _a = value;
            UpdatePosition();
        }
    }
    public float width
    {
        get
        {
            return _s.y;
        }
        set
        {
            _s.y = value;
            UpdatePosition();
        }
    }
    protected override void Start()
    {
        base.Start();
        _p = _cP;
        _s = _cS;
        _a = _cA;
        UpdatePosition();
    }
    private void UpdatePosition()
    {
        rectTransform.localRotation = Quaternion.Euler(0, 0, _a);
        rectTransform.pivot = new Vector2(_s.y / 2f / _s.x, 0.5f);
        rectTransform.localPosition = _p;
        rectTransform.sizeDelta = _s;
    }
    public static void setCreationValues(Vector2 start, float lineLength, float lineWidth, float lineAngle)
    {
        _cP = start;
        _cS = new Vector2(lineLength, lineWidth);
        _cA = lineAngle;
    }
}