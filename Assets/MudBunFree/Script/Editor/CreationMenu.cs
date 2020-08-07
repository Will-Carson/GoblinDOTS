/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

using System.IO;

using UnityEditor;
using UnityEngine;

namespace MudBun
{
  public class CreationMenu
  {
    [MenuItem("GameObject/Mud Bun")]
    public static void SubMenu() { }

    protected static GameObject CreateGameObject(string name)
    {
      var go = new GameObject(name);

      var selectedGo = Selection.activeGameObject;
      if (selectedGo != null)
      {
        go.transform.parent = selectedGo.transform;
        go.transform.localPosition = Vector3.zero;
      }

      Undo.RegisterCreatedObjectUndo(go, go.name);

      return go;
    }

    protected static GameObject OnBrushCreated(GameObject go, bool setAsFirstChild = false)
    {
      bool parentedUnderRenderer = false;
      var t = go.transform.parent;
      while (t != null)
      {
        if (t.GetComponent<MudRenderer>() != null)
        {
          parentedUnderRenderer = true;
          break;
        }
        t = t.parent;
      }

      if (!parentedUnderRenderer)
      {
        var renderer = new GameObject("Mud Renderer");
        renderer.AddComponent<MudRenderer>();
        if (go.transform.parent != null)
          renderer.transform.parent = go.transform.parent;

        go.transform.parent = renderer.transform;

        Undo.RegisterCreatedObjectUndo(renderer, renderer.name);
      }

      if (setAsFirstChild)
        go.transform.SetSiblingIndex(0);

      Selection.activeGameObject = go;

      return go;
    }

    // https://gist.github.com/allanolivei/9260107
    public static string GetSelectedPathOrFallback()
    {
      string path = "Assets";

      foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
      {
        path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
          path = Path.GetDirectoryName(path);
          break;
        }
      }

      return path;
    }

    [MenuItem("GameObject/Mud Bun/Renderer", priority = 4)]
    public static GameObject CreateRenderer()
    {
      var go = CreateGameObject("Mud Renderer");
      go.AddComponent<MudRenderer>();

      Selection.activeGameObject = go;

      return go;
    }

    [MenuItem("GameObject/Mud Bun/Primitives/Box", priority = 3)]
    public static GameObject CreateBox()
    {
      var go = CreateGameObject("Mud Box");
      go.AddComponent<MudBox>();

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Primitives/Sphere", priority = 3)]
    public static GameObject CreateSphere()
    {
      var go = CreateGameObject("Mud Sphere");
      go.AddComponent<MudSphere>();

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Primitives/Cylinder", priority = 3)]
    public static GameObject CreateCylinder()
    {
      var go = CreateGameObject("Mud Cylinder");
      go.AddComponent<MudCylinder>();

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Primitives/Cone", priority = 3)]
    public static GameObject CreateCone()
    {
      var go = CreateGameObject("Mud Cone");
      var comp = go.AddComponent<MudCone>();

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Primitives/Torus", priority = 3)]
    public static GameObject CreateTorus()
    {
      var go = CreateGameObject("Mud Torus");
      go.AddComponent<MudTorus>();

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Primitives/Solid Angle", priority = 3)]
    public static GameObject CreateSolidAngle()
    {
      var go = CreateGameObject("Mud Solid Angle");
      go.AddComponent<MudSolidAngle>();

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Primitives/Curve (Simple: 2 Points + 1 Control)", priority = 3)]
    public static GameObject CreateCurveSimple()
    {
      var go = CreateGameObject("Mud Curve (Simple)");
      var comp = go.AddComponent<MudCurveSimple>();
      comp.EnableNoise = false;

      var pA = CreateGameObject("Curve Point A");
      pA.transform.parent = go.transform;
      pA.transform.localPosition = new Vector3(-0.5f, 0.0f);

      var pC = CreateGameObject("Curve Control Point");
      pC.transform.parent = go.transform;
      pC.transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);

      var pB = CreateGameObject("Curve Point B");
      pB.transform.parent = go.transform;
      pB.transform.localPosition = new Vector3(0.5f, 0.0f, 0.0f);

      comp.PointA = pA.transform;
      comp.ControlPoint = pC.transform;
      comp.PointB = pB.transform;

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Primitives/Curve (Full: Any Points)", priority = 3)]
    public static GameObject CreateCurveFull()
    {
      var go = CreateGameObject("Mud Curve (Full)");
      var comp = go.AddComponent<MudCurveFull>();
      comp.EnableNoise = false;

      var p0 = CreateGameObject("Curve Point (0)");
      p0.transform.parent = go.transform;
      p0.transform.localPosition = new Vector3(-0.5f, 0.0f);

      var p1 = CreateGameObject("Curve Point (1)");
      p1.transform.parent = go.transform;
      p1.transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);

      var p2 = CreateGameObject("Curve Point (2)");
      p2.transform.parent = go.transform;
      p2.transform.localPosition = new Vector3(0.5f, 0.0f, 0.0f);

      comp.Points = new MudCurveFull.Point[]
      {
        new MudCurveFull.Point(p0, 0.2f), 
        new MudCurveFull.Point(p1, 0.2f), 
        new MudCurveFull.Point(p2, 0.2f), 
      };

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Effects/Particle System", priority = 3)]
    public static GameObject CreateParticleSystem()
    {
      var go = CreateGameObject("Mud Particle System");
      go.transform.rotation = Quaternion.AngleAxis(-90.0f, Vector3.right);

      var comp = go.AddComponent<MudParticleSystem>();
      var particles = go.AddComponent<ParticleSystem>();
      comp.Particles = particles;

      var main = particles.main;
      main.simulationSpace = ParticleSystemSimulationSpace.World;
      main.startLifetime = 2.0f;
      main.startSpeed = 2.0f;
      main.startSize = 0.1f;

      var shape = particles.shape;
      shape.enabled = true;
      shape.angle = 15.0f;
      shape.radius = 0.0f;

      var size = particles.sizeOverLifetime;
      size.enabled = true;
      size.size =
        new ParticleSystem.MinMaxCurve
        (
          1.0f,
          new AnimationCurve
          (
            new Keyframe[]
            {
              new Keyframe(0.0f, 0.0f),
              new Keyframe(0.2f, 1.0f),
              new Keyframe(0.8f, 1.0f),
              new Keyframe(1.0f, 0.0f),
            }
          )
        );

      var renderer = go.GetComponent<ParticleSystemRenderer>();
      renderer.enabled = false;

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Effects/Noise Volume", priority = 3)]
    public static GameObject CreateNoiseeVolume()
    {
      var go = CreateGameObject("Mud Noise");
      go.AddComponent<MudNoiseVolume>();

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Effects/Noise Curve (Simple: 2 Points + 1 Control)", priority = 3)]
    public static GameObject CreateNoiseCurveSimple()
    {
      var go = CreateGameObject("Mud Curve (Simple)");
      var comp = go.AddComponent<MudCurveSimple>();
      comp.EnableNoise = true;

      var pA = CreateGameObject("Curve Point A");
      pA.transform.parent = go.transform;
      pA.transform.localPosition = new Vector3(-0.5f, 0.0f);

      var pC = CreateGameObject("Curve Control Point");
      pC.transform.parent = go.transform;
      pC.transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);

      var pB = CreateGameObject("Curve Point B");
      pB.transform.parent = go.transform;
      pB.transform.localPosition = new Vector3(0.5f, 0.0f, 0.0f);

      comp.PointA = pA.transform;
      comp.ControlPoint = pC.transform;
      comp.PointB = pB.transform;

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Effects/Noise Curve (Full: Any Points)", priority = 3)]
    public static GameObject CreateNoiseCurveFull()
    {
      var go = CreateGameObject("Mud Curve (Full)");
      var comp = go.AddComponent<MudCurveFull>();
      comp.EnableNoise = true;

      var p0 = CreateGameObject("Curve Point (0)");
      p0.transform.parent = go.transform;
      p0.transform.localPosition = new Vector3(-0.5f, 0.0f);

      var p1 = CreateGameObject("Curve Point (1)");
      p1.transform.parent = go.transform;
      p1.transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);

      var p2 = CreateGameObject("Curve Point (2)");
      p2.transform.parent = go.transform;
      p2.transform.localPosition = new Vector3(0.5f, 0.0f, 0.0f);

      comp.Points = new MudCurveFull.Point[]
      {
        new MudCurveFull.Point(p0, 0.2f), 
        new MudCurveFull.Point(p1, 0.2f), 
        new MudCurveFull.Point(p2, 0.2f), 
      };

      return OnBrushCreated(go);
    }

    [MenuItem("GameObject/Mud Bun/Distortion/Fish Eye", priority = 3)]
    public static GameObject CreateFishEye()
    {
      var go = CreateGameObject("Mud Fish Eye");
      go.AddComponent<MudFishEye>();

      return OnBrushCreated(go, true);
    }

    [MenuItem("GameObject/Mud Bun/Distortion/Twist", priority = 3)]
    public static GameObject CreateTwist()
    {
      var go = CreateGameObject("Mud Twist");
      go.AddComponent<MudTwist>();

      go.transform.localScale = new Vector3(1.0f, 1.2f, 1.0f);

      return OnBrushCreated(go, true);
    }

    [MenuItem("GameObject/Mud Bun/Distortion/Pinch", priority = 3)]
    public static GameObject CreatePinch()
    {
      var go = CreateGameObject("Mud Pinch");
      go.AddComponent<MudPinch>();

      return OnBrushCreated(go, true);
    }

    [MenuItem("GameObject/Mud Bun/Distortion/Quantize", priority = 3)]
    public static GameObject CreateQuantize()
    {
      var go = CreateGameObject("Mud Quantize");
      go.AddComponent<MudQuantize>();

      return OnBrushCreated(go, true);
    }

    [MenuItem("GameObject/Mud Bun/Modifiers/Onion", priority = 3)]
    public static GameObject CreateOnion()
    {
      var go = CreateGameObject("Mud Onion");
      go.AddComponent<MudOnion>();

      return OnBrushCreated(go);
    }

    [MenuItem("Assets/Create/MudBun/Mesh Renderer Material (Single-Textured)", priority = 151)]
    public static void CreateMeshSingleTexturedRendererMaterial()
    {
      string path = GetSelectedPathOrFallback() + "/MudBun Mesh Single-Textured Renderer Material.mat";

      Material mat = new Material(ResourcesUtil.DefaultMeshSingleTexturedMaterial);
      if (mat == null)
        return;

      ProjectWindowUtil.CreateAsset(mat, path);
    }

    [MenuItem("Assets/Create/MudBun/Mesh Renderer Material (Multi-Textured)", priority = 151)]
    public static void CreateMeshMultiTexturedRendererMaterial()
    {
      string path = GetSelectedPathOrFallback() + "/MudBun Mesh Multi-Textured Renderer Material.mat";

      Material mat = new Material(ResourcesUtil.DefaultMeshMultiTexturedMaterial);
      if (mat == null)
        return;

      ProjectWindowUtil.CreateAsset(mat, path);
    }

    [MenuItem("Assets/Create/MudBun/Splat Renderer Material (Single-Textured)", priority = 151)]
    public static void CreateSplatSingleTexturedRendererMaterial()
    {
      string path = GetSelectedPathOrFallback() + "/MudBun Splat Single-Textured Renderer Material.mat";

      Material mat = new Material(ResourcesUtil.DefaultSplatSingleTexturedMaterial);
      if (mat == null)
        return;

      ProjectWindowUtil.CreateAsset(mat, path);
    }

    [MenuItem("Assets/Create/MudBun/Splat Renderer Material (Multi-Textured)", priority = 151)]
    public static void CreateSplatMultiTexturedRendererMaterial()
    {
      string path = GetSelectedPathOrFallback() + "/MudBun Splat Multi-Textured Renderer Material.mat";

      Material mat = new Material(ResourcesUtil.DefaultSplatMultiTexturedMaterial);
      if (mat == null)
        return;

      ProjectWindowUtil.CreateAsset(mat, path);
    }

    [MenuItem("Assets/Create/MudBun/Standard Mesh Material (for Locked Mesh)", priority = 151)]
    public static void CreateStandardMeshMaterial()
    {
      string path = GetSelectedPathOrFallback() + "/MudBun Standard Mesh Material.mat";

      Material mat = new Material(ResourcesUtil.DefaultStandardMeshMaterial);
      if (mat == null)
        return;

      ProjectWindowUtil.CreateAsset(mat, path);
    }
  }
}

