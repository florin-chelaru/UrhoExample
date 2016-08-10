using Urho;
using Urho.Forms;
using Xamarin.Forms;

namespace UrhoExample
{
  public class UrhoPage : ContentPage
  {
    UrhoSurface urhoSurface;
    UrhoApp urhoApp;

    static readonly string[] textures = { "tile-200.png", "tile-256.png", "tile-500.png", "tile-512.png", "tile-1000.png", "tile-1024.png", "tile-2000.png", "tile-2048.png" };

    Picker shapePicker;
    Picker texturePicker;

    public UrhoPage()
    {
      urhoSurface = new UrhoSurface { VerticalOptions = LayoutOptions.FillAndExpand };

      texturePicker = new Picker();
      foreach (var tex in textures) { texturePicker.Items.Add(tex); }

      string[] shapes = { "Custom Geometry", "Sphere" };
      shapePicker = new Picker();
      foreach (var shape in shapes) { shapePicker.Items.Add(shape); }

      texturePicker.SelectedIndexChanged += (sender, e) =>
      {
        if (urhoApp == null) { return; }
        urhoApp.SelectedTexture = textures[texturePicker.SelectedIndex];
      };

      shapePicker.SelectedIndexChanged += (sender, e) =>
      {
        if (urhoApp == null) { return; }
        urhoApp.UseSphere = (shapePicker.SelectedIndex == 1);
      };

      texturePicker.SelectedIndex = 3;
      shapePicker.SelectedIndex = 0;

      Title = "Urho Example";
      Content = new StackLayout
      {
        Padding = 0,
        VerticalOptions = LayoutOptions.FillAndExpand,
        Children = 
        {
          urhoSurface,
          new Label { Text = "Texture:" },
          texturePicker,
          new Label { Text = "Model:" },
          shapePicker
        }
      };
    }

    protected override async void OnAppearing()
    {
      urhoApp = await urhoSurface.Show<UrhoApp>(
        new ApplicationOptions("Data") { Orientation = ApplicationOptions.OrientationType.Portrait });
    }
  }
}


