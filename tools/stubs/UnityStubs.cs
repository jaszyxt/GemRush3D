// Unity API stubs for OFFLINE SYNTAX CHECKING ONLY.
// These mirror the small slice of the Unity API that GemRush uses, closely
// enough for the legacy Windows C# compiler (csc.exe) to type-check the game
// scripts without an installed editor. Never shipped or compiled by Unity
// (this folder lives outside Assets/).

using System;

namespace UnityEngine
{
    // ---------- Object model ----------

    public class Object
    {
        public string name;
        public static void Destroy(Object obj) { }
        public static void Destroy(Object obj, float delay) { }
        public static void DontDestroyOnLoad(Object obj) { }
        public static T FindObjectOfType<T>() where T : Object { return default(T); }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public class Component : Object
    {
        public GameObject gameObject { get { return null; } }
        public Transform transform { get { return null; } }
        public T GetComponent<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T GetComponentInChildren<T>() { return default(T); }
    }

    public class Behaviour : Component
    {
        public bool enabled;
    }

    public class MonoBehaviour : Behaviour { }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public static GameObject CreatePrimitive(PrimitiveType type) { return null; }
        public static GameObject Find(string name) { return null; }
        public Transform transform { get { return null; } }
        public string tag { get; set; }
        public bool activeSelf { get { return false; } }
        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component { return default(T); }
        public T GetComponent<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
    }

    public class Transform : Component, System.Collections.IEnumerable
    {
        public Transform parent { get { return null; } }
        public int childCount { get { return 0; } }
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Quaternion localRotation { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 forward { get { return new Vector3(); } }
        public Vector3 right { get { return new Vector3(); } }
        public Vector3 up { get { return new Vector3(); } }
        public Transform GetChild(int index) { return null; }
        public Transform Find(string name) { return null; }
        public System.Collections.IEnumerator GetEnumerator() { return null; }
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void LookAt(Vector3 worldPoint) { }
        public void Rotate(Vector3 eulers) { }
        public void Rotate(Vector3 axis, float angle) { }
        public void Rotate(Vector3 axis, float angle, Space relativeTo) { }
        public void Translate(Vector3 translation) { }
        public Vector3 InverseTransformDirection(Vector3 worldDirection) { return new Vector3(); }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 pivot { get; set; }
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum Space { World, Self }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    public enum RuntimeInitializeLoadType
    {
        BeforeSceneLoad,
        AfterSceneLoad,
        BeforeSplashScreen,
        AfterAssembliesLoaded
    }

    // ---------- Math & geometry ----------

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float sqrMagnitude { get { return 0f; } }
        public float magnitude { get { return 0f; } }
        public Vector2 normalized { get { return this; } }
        public static Vector2 zero { get { return new Vector2(); } }
        public static Vector2 one { get { return new Vector2(); } }
        public static float Distance(Vector2 a, Vector2 b) { return 0f; }
        public static bool operator ==(Vector2 a, Vector2 b) { return true; }
        public static bool operator !=(Vector2 a, Vector2 b) { return false; }
        public override bool Equals(object other) { return true; }
        public override int GetHashCode() { return 0; }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return a; }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return a; }
        public static Vector2 operator *(Vector2 a, float d) { return a; }
        public static Vector2 operator /(Vector2 a, float d) { return a; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude { get { return 0f; } }
        public float magnitude { get { return 0f; } }
        public Vector3 normalized { get { return this; } }
        public static Vector3 zero { get { return new Vector3(); } }
        public static Vector3 one { get { return new Vector3(); } }
        public static Vector3 up { get { return new Vector3(); } }
        public static Vector3 down { get { return new Vector3(); } }
        public static Vector3 forward { get { return new Vector3(); } }
        public static Vector3 right { get { return new Vector3(); } }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return a; }
        public static float Distance(Vector3 a, Vector3 b) { return 0f; }
        public void Normalize() { }
        public static bool operator ==(Vector3 a, Vector3 b) { return true; }
        public static bool operator !=(Vector3 a, Vector3 b) { return false; }
        public override bool Equals(object other) { return true; }
        public override int GetHashCode() { return 0; }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator *(Vector3 a, float d) { return a; }
        public static Vector3 operator /(Vector3 a, float d) { return a; }
    }

    public struct Quaternion
    {
        public static Quaternion identity { get { return new Quaternion(); } }
        public static Quaternion Euler(float x, float y, float z) { return new Quaternion(); }
        public static Quaternion operator *(Quaternion a, Quaternion b) { return a; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(); } }
        public static Color black { get { return new Color(); } }
        public static Color red { get { return new Color(); } }
        public static Color green { get { return new Color(); } }
        public static Color blue { get { return new Color(); } }
        public static Color Lerp(Color a, Color b, float t) { return a; }
        public static Color operator *(Color c, float f) { return c; }
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public static float Sin(float f) { return 0f; }
        public static float Cos(float f) { return 0f; }
        public static float Sqrt(float f) { return 0f; }
        public static float Abs(float f) { return 0f; }
        public static int Abs(int value) { return 0; }
        public static float Exp(float f) { return 0f; }
        public static float Lerp(float a, float b, float t) { return 0f; }
        public static float Clamp01(float value) { return 0f; }
        public static float Clamp(float value, float min, float max) { return 0f; }
        public static int Clamp(int value, int min, int max) { return 0; }
        public static float Min(float a, float b) { return 0f; }
        public static float Max(float a, float b) { return 0f; }
        public static int Max(int a, int b) { return 0; }
        public static int Min(int a, int b) { return 0; }
        public static float SmoothStep(float from, float to, float t) { return 0f; }
        public static int RoundToInt(float f) { return 0; }
        public static float Repeat(float t, float length) { return 0f; }
        public static float MoveTowards(float current, float target,
            float maxDelta) { return 0f; }
    }

    public static class PlayerPrefs
    {
        public static int GetInt(string key, int defaultValue) { return 0; }
        public static void SetInt(string key, int value) { }
        public static float GetFloat(string key, float defaultValue) { return 0f; }
        public static void SetFloat(string key, float value) { }
        public static string GetString(string key, string defaultValue) { return null; }
        public static void SetString(string key, string value) { }
        public static bool HasKey(string key) { return false; }
        public static void DeleteKey(string key) { }
        public static void Save() { }
    }

    public static class Random
    {
        public static float value { get { return 0f; } }
        public static float Range(float min, float max) { return 0f; }
        public static int Range(int min, int max) { return 0; }
    }

    public static class Time
    {
        public static float time { get { return 0f; } }
        public static float deltaTime { get { return 0f; } }
        public static float fixedDeltaTime { get { return 0f; } }
        public static float maximumDeltaTime { get; set; }
        public static float timeSinceLevelLoad { get { return 0f; } }
        public static float timeScale { get; set; }
        public static float unscaledDeltaTime { get { return 0f; } }
        public static float unscaledTime { get { return 0f; } }
    }

    // ---------- Input ----------

    public static class Input
    {
        public static float GetAxisRaw(string axisName) { return 0f; }
        public static bool GetButton(string buttonName) { return false; }
        public static bool GetButtonDown(string buttonName) { return false; }
        public static bool GetKey(KeyCode key) { return false; }
        public static bool GetKeyDown(KeyCode key) { return false; }
        public static bool touchSupported { get { return false; } }
        public static int touchCount { get { return 0; } }
        public static Touch GetTouch(int index) { return new Touch(); }
    }

    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }

    public struct Touch
    {
        public int fingerId;
        public Vector2 position;
        public Vector2 deltaPosition;
        public TouchPhase phase;
    }

    public enum ScreenOrientation
    {
        Portrait, PortraitUpsideDown, Landscape, LandscapeLeft, LandscapeRight, AutoRotation
    }

    public static class Screen
    {
        public static ScreenOrientation orientation { get; set; }
        public static int sleepTimeout { get; set; }
        public static int width { get { return 0; } }
        public static int height { get { return 0; } }
    }

    public static class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }

    public enum KeyCode
    {
        None, Space, Return, Escape, KeypadEnter,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z
    }

    // ---------- Physics ----------

    public static class Physics
    {
        public const int DefaultRaycastLayers = -5;
        public static Collider[] OverlapSphere(Vector3 position, float radius,
            int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        { return null; }
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents,
            Quaternion orientation, int layerMask,
            QueryTriggerInteraction queryTriggerInteraction)
        { return null; }
    }

    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }

    public class Collider : Component
    {
        public bool isTrigger { get; set; }
        public Rigidbody attachedRigidbody { get { return null; } }
    }

    public class BoxCollider : Collider
    {
        public Vector3 size { get; set; }
        public Vector3 center { get; set; }
    }

    public class SphereCollider : Collider
    {
        public float radius { get; set; }
        public Vector3 center { get; set; }
    }

    public class CapsuleCollider : Collider
    {
        public float height { get; set; }
        public float radius { get; set; }
        public Vector3 center { get; set; }
    }

    public class ContactPoint
    {
        public Vector3 normal { get { return new Vector3(); } }
        public Collider collider { get { return null; } }
    }

    public class Collision
    {
        public ContactPoint[] contacts { get { return null; } }
        public GameObject gameObject { get { return null; } }
    }

    public class Rigidbody : Component
    {
        public Vector3 velocity { get; set; }
        public Vector3 linearVelocity { get; set; }
        public Vector3 position { get; set; }
        public float mass { get; set; }
        public float drag { get; set; }
        public float linearDamping { get; set; }
        public bool isKinematic { get; set; }
        public bool useGravity { get; set; }
        public RigidbodyConstraints constraints { get; set; }
        public RigidbodyInterpolation interpolation { get; set; }
        public CollisionDetectionMode collisionDetectionMode { get; set; }
        public void MovePosition(Vector3 position) { }
    }

    public enum RigidbodyConstraints { None, FreezePosition, FreezeRotation }
    public enum RigidbodyInterpolation { None, Interpolate, Extrapolate }
    public enum CollisionDetectionMode { Discrete, Continuous, ContinuousDynamic }

    // ---------- Rendering ----------

    public class Shader : Object
    {
        public static Shader Find(string name) { return null; }
    }

    public enum MaterialGlobalIlluminationFlags { None, RealtimeEmissive, BakedEmissive, EmissiveIsBlack }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public int renderQueue { get; set; }
        public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        public void SetColor(string name, Color value) { }
        public void SetFloat(string name, float value) { }
        public void SetInt(string name, int value) { }
        public void EnableKeyword(string keyword) { }
        public void DisableKeyword(string keyword) { }
    }

    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; }
        public Material material { get; set; }
    }

    public class MeshRenderer : Renderer { }
    public class ParticleSystemRenderer : Renderer { }

    public class Camera : Behaviour
    {
        public static Camera main { get { return null; } }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public float farClipPlane { get; set; }
    }

    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }

    public enum LightType { Spot, Directional, Point, Area }
    public enum LightShadows { None, Hard, Soft }
    public enum ShadowQuality { All, HardOnly, SoftOnly, Disable }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
    }

    public enum FogMode { Linear, Exponential, ExponentialSquared, Squared }

    public static class RenderSettings
    {
        public static Material skybox { get; set; }
        public static bool fog { get; set; }
        public static FogMode fogMode { get; set; }
        public static float fogDensity { get; set; }
        public static Color fogColor { get; set; }
        public static UnityEngine.Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
    }

    public static class QualitySettings
    {
        public static int antiAliasing { get; set; }
        public static ShadowQuality shadows { get; set; }
    }

    public static class Application
    {
        public static void Quit() { }
        public static bool isPlaying { get { return false; } }
        public static int targetFrameRate { get; set; }
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object { return default(T); }
    }

    public struct LayerMask
    {
        public static implicit operator LayerMask(int intVal) { return new LayerMask(); }
        public static implicit operator int(LayerMask mask) { return 0; }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject { return default(T); }
    }

    public class TextMesh : Component
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public float characterSize { get; set; }
        public Color color { get; set; }
        public TextAnchor anchor { get; set; }
        public TextAlignment alignment { get; set; }
    }

    public enum TextAlignment { Left, Center, Right }

    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }

    public class Font : Object
    {
        public Material material { get; set; }
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w)
        { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    // ---------- Post-processing (com.unity.postprocessing) ----------

    namespace Rendering.PostProcessing
    {
        public class PostProcessEffectSettings : ScriptableObject
        {
            public BoolParameter enabled { get; set; }
        }

        public class BoolParameter
        {
            public bool value { get; set; }
            public bool overrideState { get; set; }
            public void Override(bool state) { }
        }

        public class FloatParameter
        {
            public float value { get; set; }
            public bool overrideState { get; set; }
            public void Override(float state) { }
        }

        public class Bloom : PostProcessEffectSettings
        {
            public FloatParameter intensity { get; set; }
            public FloatParameter threshold { get; set; }
            public FloatParameter softKnee { get; set; }
            public FloatParameter diffusion { get; set; }
        }

        public class Vignette : PostProcessEffectSettings
        {
            public FloatParameter intensity { get; set; }
            public FloatParameter smoothness { get; set; }
        }

        public class ColorGrading : PostProcessEffectSettings
        {
            public FloatParameter saturation { get; set; }
            public FloatParameter contrast { get; set; }
        }

        public class PostProcessProfile : ScriptableObject
        {
            public T AddSettings<T>() where T : PostProcessEffectSettings, new() { return new T(); }
        }

        public class PostProcessVolume : Behaviour
        {
            public bool isGlobal { get; set; }
            public PostProcessProfile profile { get; set; }
        }

        public class PostProcessLayer : Behaviour
        {
            public LayerMask volumeLayer { get; set; }
            public void Init(PostProcessVolume defaultVolume) { }
        }
    }

    // ---------- Sprites & textures ----------

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { }
    }

    public enum TextureFormat { Alpha8, RGB24, RGBA32, ARGB32 }

    public class Texture : Object { }

    public class Texture2D : Texture
    {
        public Texture2D(int width, int height) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public void SetPixel(int x, int y, Color color) { }
        public void Apply() { }
    }

    public class Sprite : Object
    {
        public static Sprite Create(Texture2D texture, Rect rect,
            Vector2 pivot, float pixelsPerUnit) { return null; }
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot,
            float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border)
        { return null; }
    }

    public enum SpriteMeshType { FullRect, Tight }

    // ---------- Audio ----------

    public class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels,
            int frequency, bool stream) { return null; }
        public void SetData(float[] data, int offsetSamples) { }
    }

    public class AudioSource : Behaviour
    {
        public bool playOnAwake { get; set; }
        public float spatialBlend { get; set; }
        public float volume { get; set; }
        public bool loop { get; set; }
        public AudioClip clip { get; set; }
        public bool isPlaying { get { return false; } }
        public void PlayOneShot(AudioClip clip) { }
        public void PlayOneShot(AudioClip clip, float volumeScale) { }
        public void Play() { }
        public void Stop() { }
    }

    public class AudioListener : Behaviour { }

    public enum ParticleSystemSimulationSpace { Local, World, Custom }
    public enum ParticleSystemShapeType { Sphere, Hemisphere, Cone, Box, Circle, Edge }

    // ---------- Particles ----------

    public class ParticleSystem : Component
    {
        public struct MinMaxCurve
        {
            public MinMaxCurve(float constant) { }
            public MinMaxCurve(float min, float max) { }
            public static implicit operator MinMaxCurve(float value) { return new MinMaxCurve(); }
        }

        public struct MinMaxGradient
        {
            public MinMaxGradient(Gradient gradient) { }
            public static implicit operator MinMaxGradient(Color color) { return new MinMaxGradient(); }
        }

        public struct Burst
        {
            public Burst(float time, short count) { }
        }

        public struct MainModule
        {
            public float duration { get; set; }
            public bool loop { get; set; }
            public MinMaxCurve startLifetime { get; set; }
            public MinMaxCurve startSpeed { get; set; }
            public MinMaxCurve startSize { get; set; }
            public MinMaxGradient startColor { get; set; }
            public MinMaxCurve gravityModifier { get; set; }
            public ParticleSystemSimulationSpace simulationSpace { get; set; }
        }

        public struct EmissionModule
        {
            public bool enabled { get; set; }
            public MinMaxCurve rateOverTime { get; set; }
            public void SetBursts(Burst[] bursts) { }
        }

        public struct ShapeModule
        {
            public bool enabled { get; set; }
            public ParticleSystemShapeType shapeType { get; set; }
            public float radius { get; set; }
        }

        public struct ColorOverLifetimeModule
        {
            public bool enabled { get; set; }
            public MinMaxGradient color { get; set; }
        }

        public MainModule main { get { return new MainModule(); } }
        public EmissionModule emission { get { return new EmissionModule(); } }
        public ShapeModule shape { get { return new ShapeModule(); } }
        public ColorOverLifetimeModule colorOverLifetime { get { return new ColorOverLifetimeModule(); } }
        public void Play() { }
        public void Stop() { }
    }

    public class Gradient
    {
        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys) { }
    }

    public struct GradientColorKey
    {
        public Color color;
        public float time;
        public GradientColorKey(Color color, float time) { this.color = color; this.time = time; }
    }

    public struct GradientAlphaKey
    {
        public float alpha;
        public float time;
        public GradientAlphaKey(float alpha, float time) { this.alpha = alpha; this.time = time; }
    }
}

namespace UnityEngine.Rendering
{
    public enum AmbientMode { Skybox, Trilight, Flat }

    // Standard GPU blend factors used by ArtLib's transparent materials.
    public enum BlendMode
    {
        One, Zero, SrcColor, SrcAlpha, DstColor, DstAlpha,
        OneMinusSrcColor, OneMinusSrcAlpha, OneMinusDstColor, OneMinusDstAlpha
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
}

namespace UnityEngine.UI
{
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }


    public class Graphic : Component
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get { return null; } }
    }

    public class Canvas : Component
    {
        public RenderMode renderMode { get; set; }
        public float scaleFactor { get { return 1f; } }
    }

    public class CanvasScaler : Component
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
    }

    public class GraphicRaycaster : Component { }

    public class Text : Graphic
    {
        public Font font { get; set; }
        public string text { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
    }

    public class Image : Graphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public Type type { get; set; }
        public Sprite sprite { get; set; }
    }

    public class Outline : Component
    {
        public Color effectColor { get; set; }
        public Vector2 effectDistance { get; set; }
    }

    public class Button : Component
    {
        public class ButtonClickedEvent
        {
            public void AddListener(UnityEngine.Events.UnityAction call) { }
        }

        public Graphic targetGraphic { get; set; }
        public bool interactable { get; set; }
        public ButtonClickedEvent onClick { get { return null; } }
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : Component { }
    public class StandaloneInputModule : Component { }
}
