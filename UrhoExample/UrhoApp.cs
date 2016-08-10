using System;
using System.Collections.Generic;
using Urho;
using Urho.Resources;
using Urho.Shapes;
using Urho.Urho2D;

namespace UrhoExample
{
  public class UrhoApp : Application
  {
    const string CustomModelName = "MyCustomModel";
    const int TileSegmentCount = 4;
    const int TileCount = 8;

    Scene scene;
    Node root;
    Camera camera;

    Sphere sphere;
    StaticModel[] tiles;

    Node sphereNode;
    Node tilesNode;

    string selectedTexture = "tile-256.png";

    public string SelectedTexture
    {
      get { return selectedTexture; }
      set
      {
        selectedTexture = value;

        InvokeOnMain(() =>
        {

          if (sphere != null) { sphere.SetMaterial(CreateMaterial(selectedTexture)); }

          if (tilesNode != null)
          {
            var material = CreateMaterial(selectedTexture);
            foreach (var tile in tiles)
            {
              tile.SetMaterial(material);
            }
          }
        });
      }
    }

    bool useSphere;

    public bool UseSphere
    {
      get { return useSphere; }
      set
      {
        useSphere = value;

        InvokeOnMain(() =>
        {
          if (useSphere) { AddSphere(); }
          else { AddTiledSphere(); }
        });
      }
    }

    public UrhoApp(ApplicationOptions options = null) : base(options) {}

    protected override void Start()
    {
      base.Start();

      AddCustomModels();

      CreateScene();
      SetupViewport();
    }

    void AddCustomModels()
    {
      var cache = ResourceCache;

      if (!cache.Exists(CustomModelName))
      {
        const int nsegments = TileSegmentCount;
        Model customModel = CreateSphereFragmentModel((float)Math.PI * 0.5f / nsegments, (float)Math.PI * 0.5f / nsegments, nsegments, nsegments);
        customModel.Name = CustomModelName;
        cache.AddManualResource(customModel);
      }
    }

    void CreateScene()
    {
      var cache = ResourceCache;

      scene = new Scene();
      scene.CreateComponent<Octree>();

      root = scene.CreateChild("root");

      var cameraNode = scene.CreateChild("camera");
      camera = cameraNode.CreateComponent<Camera>();
      cameraNode.Position = new Vector3(0, 0, 0);
      camera.Zoom = 0.1f;

      var lightNode = cameraNode.CreateChild("light");
      lightNode.Position = new Vector3(0, 0, 0);
      lightNode.SetDirection(new Vector3(0f, 0f, -1f));

      var light = lightNode.CreateComponent<Light>();
      light.LightType = LightType.Directional;
      light.Range = 100f;
      light.SpecularIntensity = 1.5f;

      if (UseSphere) { AddSphere(); }
      else { AddTiledSphere(); }
    }

    void AddSphere()
    {
      if (root == null) { return; }

      RemoveSphere();
      RemoveTiledSphere();

      sphereNode = root.CreateChild("sphere");
      sphereNode.Position = new Vector3(0f, 0f, 0f);
      sphereNode.SetScale(4f);

      sphere = sphereNode.CreateComponent<Sphere>();

      if (SelectedTexture != null)
      {
        sphere.SetMaterial(CreateMaterial(SelectedTexture));
      }
    }

    void RemoveSphere()
    {
      if (sphereNode != null)
      {
        sphereNode.Remove();
      }
      sphereNode = null;
      sphere = null;
    }

    void RemoveTiledSphere()
    {
      if (tilesNode != null)
      {
        tilesNode.Remove();
      }
      tilesNode = null;
      tiles = null;
    }

    void AddTiledSphere()
    {
      if (root == null) { return; }

      RemoveSphere();
      RemoveTiledSphere();

      const int ntiles = TileCount;
      const int halfNtiles = ntiles / 2;

      var material = SelectedTexture != null ? CreateMaterial(SelectedTexture) : null;

      var model = ResourceCache.GetModel(CustomModelName);
      tiles = new StaticModel[ntiles];

      tilesNode = root.CreateChild("tiles");
      for (int i = 0; i < ntiles; ++i)
      {
        var tileNode = tilesNode.CreateChild("tile" + i);
        tileNode.Position = new Vector3(0f, 0f, 0f);
        tileNode.SetScale(4f);
        tileNode.Rotation = Quaternion.Multiply(
          QuaternionExtensions.FromAxisAngle(new Vector3(0f, 1f, 0f), (float)((i % halfNtiles) * Math.PI * 0.5)),
          QuaternionExtensions.FromAxisAngle(new Vector3(0f, 0f, 1f), (float)((i / halfNtiles) * Math.PI)));

        var tile = tileNode.CreateComponent<StaticModel>();
        tile.Model = model;
        tile.SetMaterial(material);
        tiles[i] = tile;
      }
    }

    Material CreateMaterial(string imagePath)
    {
      var cache = ResourceCache;
      var material = new Material();
      material.SetTechnique(0, cache.GetTechnique("Techniques/Diff.xml"), 0, 0f);
      material.CullMode = CullMode.None; // Material -> double-sided
      var texture = new Texture2D();
      texture.SetData(cache.GetImage(imagePath), false);
      material.SetTexture(TextureUnit.Diffuse, texture);

      return material;
    }

    protected override void OnUpdate(float timeStep)
    {
      if (camera?.Node != null)
      {
        camera.Node.Rotate(new Quaternion(0, 1f, 0), TransformSpace.Local);
      }
      base.OnUpdate(timeStep);
    }

    void SetupViewport()
    {
      var renderer = Renderer;
      renderer.SetViewport(0, new Viewport(Context, scene, camera, null));
    }

    /// <summary>
    /// Uses and adapts the example code from the following sources to create a sphere tile
    /// see cref="https://github.com/urho3d/Urho3D/blob/master/Source/Samples/34_DynamicGeometry/DynamicGeometry.cpp"/>
    /// and
    /// see cref="https://github.com/urho3d/Urho3D/issues/470"/>
    /// </summary>
    /// <returns>Model</returns>
    /// <param name="angleX">Angle x.</param>
    /// <param name="angleY">Angle y.</param>
    /// <param name="xsegments">Xsegments.</param>
    /// <param name="ysegments">Ysegments.</param>
    /// <param name="context">Context.</param>
    public static Model CreateSphereFragmentModel(float angleX, float angleY, int xsegments = 4, int ysegments = 4, Context context = null)
    {
      context = context ?? CurrentContext;

      float tilex = 1f / xsegments;
      float tiley = 1f / ysegments;

      const float radius = 1f;

      Func<int, int, Tuple<Vector3, Vector2>> getVertex = (j, i) =>
      {
        double r = Math.Abs(radius * Math.Sin(angleY * j));

        return new Tuple<Vector3, Vector2>(
          new Vector3(
            (float)(r * Math.Cos(angleX * i)), // x
            (float)(radius * Math.Cos(angleY * j)), // y
            (float)(r * Math.Sin(angleX * i))), // z
          new Vector2(
            tilex * i, // x tex
            tiley * j)); // y tex
      };
      var vertexData = new List<float>();
      //Action<Tuple<Vector3, Vector2>, Vector3> addVertexData = (t, n) => vertexData.AddRange(new float[] { t.Item1.X, t.Item1.Y, t.Item1.Z, n.X, n.Y, n.Z, t.Item2.X, t.Item2.Y, t.Item2.X, t.Item2.Y });
      Action<Tuple<Vector3, Vector2>, Vector3> addVertexData = (t, n) => vertexData.AddRange(new float[] { t.Item1.X, t.Item1.Y, t.Item1.Z, n.X, n.Y, n.Z, t.Item2.X, t.Item2.Y });
      for (var j = 0; j < ysegments; ++j)
      {
        for (var i = 0; i < xsegments; ++i)
        {
          var a = getVertex(j, i);
          var b = getVertex(j + 1, i + 1);
          var c = getVertex(j + 1, i);
          var d = getVertex(j, i + 1);

          Vector3 ba = a.Item1 - b.Item1;
          Vector3 ca = a.Item1 - c.Item1;

          Vector3 n = Vector3.Normalize(Vector3.Cross(ba, ca));

          addVertexData(a, n);
          addVertexData(b, n);
          addVertexData(c, n);

          addVertexData(a, n);
          addVertexData(d, n);
          addVertexData(b, n);
        }
      }

      short[] indexData = new short[vertexData.Count];
      for (short i = 0; i < vertexData.Count; ++i) { indexData[i] = i; }

      Model model = new Model(context);
      VertexBuffer vb = new VertexBuffer(context, false);
      IndexBuffer ib = new IndexBuffer(context, false);

      Geometry geom = new Geometry(context);

      // Shadowed buffer needed for raycasts to work, and so that data can be automatically restored on device loss
      vb.Shadowed = true;
      //vb.SetSize((uint)vertexData.Count, ElementMask.Position | ElementMask.Normal | ElementMask.TexCoord1 | ElementMask.TexCoord2, false);
      vb.SetSize((uint)vertexData.Count, ElementMask.Position | ElementMask.Normal | ElementMask.TexCoord1 , false);
      vb.SetData(vertexData.ToArray());

      ib.Shadowed = true;
      ib.SetSize((uint)vertexData.Count, false, false);
      ib.SetData(indexData);

      geom.SetVertexBuffer(0, vb);
      geom.IndexBuffer = ib;
      geom.SetDrawRange(PrimitiveType.TriangleList, 0, (uint)vertexData.Count, true);

      model.NumGeometries = 1;
      model.SetGeometry(0, 0, geom);
      model.BoundingBox = new BoundingBox(new Vector3(-radius, -radius, -radius), new Vector3(radius, radius, radius));

      return model;
    }
  }
}
