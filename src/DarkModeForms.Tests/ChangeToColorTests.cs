using System.Drawing;
using DarkModeForms;
using Xunit;

namespace DarkModeForms.Tests;

/// <summary>
/// Guards the tinting logic in <see cref="DarkModeCS.ChangeToColor(Bitmap, Color)"/>.
/// The colour matrix is additive, so a black source pixel tinted with colour C must
/// come out as (approximately) C, while the alpha channel is left untouched.
/// </summary>
public class ChangeToColorTests
{
    [Fact]
    public void TintsBlackBitmapToRequestedColor()
    {
        using var source = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(source))
        {
            g.Clear(Color.Black);
        }

        Color target = Color.FromArgb(255, 200, 30, 40);

        using Bitmap result = DarkModeCS.ChangeToColor(source, target);

        Assert.Equal(source.Width, result.Width);
        Assert.Equal(source.Height, result.Height);

        Color center = result.GetPixel(8, 8);
        Assert.Equal(255, center.A);
        Assert.InRange(center.R, 197, 203);
        Assert.InRange(center.G, 27, 33);
        Assert.InRange(center.B, 37, 43);
    }

    [Fact]
    public void ImageOverload_AcceptsBitmapWithoutThrowing()
    {
        // Regression guard: the Image overload used to hard-cast to Bitmap, which threw
        // InvalidCastException during painting when a ToolStrip icon was not a Bitmap.
        using var source = new Bitmap(8, 8);
        using (var g = Graphics.FromImage(source))
        {
            g.Clear(Color.White);
        }

        using Image result = DarkModeCS.ChangeToColor((Image)source, Color.Red);

        Assert.IsType<Bitmap>(result);
        Assert.Equal(8, result.Width);
        Assert.Equal(8, result.Height);
    }

    [Fact]
    public void ResultIsANewBitmapInstance()
    {
        using var source = new Bitmap(4, 4);
        using (var g = Graphics.FromImage(source))
        {
            g.Clear(Color.Gray);
        }

        using Bitmap result = DarkModeCS.ChangeToColor(source, Color.Blue);

        Assert.NotSame(source, result);
    }
}
