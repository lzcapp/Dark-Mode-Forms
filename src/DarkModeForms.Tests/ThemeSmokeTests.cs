using System.Drawing;
using System.Windows.Forms;
using DarkModeForms;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DarkModeForms.Tests;

/// <summary>
/// Smoke tests that build real Forms and verify DarkModeCS themes them without
/// throwing and with the documented invariants (e.g. a Label copies its parent's
/// BackColor). WinForms controls need an STA thread and a real window for the
/// Load path, so every test body runs on a dedicated STA thread.
/// </summary>
public class ThemeSmokeTests
{
    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null)
        {
            throw failure;
        }
    }

    [Fact]
    public void ThemeControl_AppliesPaletteToNestedControls() => RunSta(() =>
    {
        using var form = new Form();
        var panel = new Panel { Dock = DockStyle.Fill, Parent = form };
        var label = new Label { Text = "Hello", Parent = panel };
        var checkBox = new CheckBox { Text = "On", Checked = true, Parent = panel };
        var textBox = new TextBox { Text = "abc", Parent = panel };
        panel.Controls.AddRange(new Control[] { label, checkBox, textBox });

        form.Controls.Add(panel);
        form.Controls.Add(new TabControl());
        form.Controls.Add(new ToolStrip());
        form.Controls.Add(new TreeView());
        form.Controls.Add(new ListView { View = View.Details });
        form.Controls.Add(new PictureBox());

        // DarkModeCS is not IDisposable: its cleanup is wired to the Form's Disposed
        // event, so disposing the form below releases the window-procedure hooks.
        var dm = new DarkModeCS(form); // SystemDefault: follows the machine's actual theme

        dm.ThemeControl(form);

        // Panels are container surfaces: ThemeControl gives them Background (not Control,
        // which is reserved for actual input controls like buttons/combos).
        Assert.Equal(dm.OScolors.Background, panel.BackColor);
        Assert.Equal(panel.BackColor, label.BackColor); // labels inherit their parent's BackColor
        Assert.Equal(panel.BackColor, checkBox.BackColor);
        Assert.Equal(dm.OScolors.TextActive, checkBox.ForeColor); // enabled controls use TextActive
        Assert.Equal(BorderStyle.None, label.BorderStyle);
    });

    [Fact]
    public void DarkModeCS_OnFormLoad_ThemesControlsAndSwitchesModes() => RunSta(() =>
    {
        using var form = new Form { Width = 320, Height = 200, Text = "Smoke" };
        var panel = new Panel { Dock = DockStyle.Fill, Parent = form };
        var label = new Label { Text = "x", Parent = panel };
        var checkBox = new CheckBox { Text = "y", Parent = panel };
        panel.Controls.AddRange(new Control[] { label, checkBox });
        form.Controls.Add(panel);

        // DarkModeCS is not IDisposable: its cleanup is wired to the Form's Disposed
        // event, so disposing the form below releases the window-procedure hooks.
        var dm = new DarkModeCS(form);

        // Fires Form.Load -> ApplyTheme(), and creates the Form's handle, which also
        // exercises the window-procedure subclassing lifecycle.
        form.Show();
        try
        {
            Assert.Equal(dm.OScolors.Background, form.BackColor);
            Assert.Equal(panel.BackColor, label.BackColor);
            Assert.Equal(panel.BackColor, checkBox.BackColor);

            // Switching modes on a loaded form re-applies the theme and must not throw.
            dm.ColorMode = DarkModeCS.DisplayMode.ClearMode;
            dm.ColorMode = DarkModeCS.DisplayMode.DarkMode;
            dm.ColorMode = DarkModeCS.DisplayMode.SystemDefault;
        }
        finally
        {
            form.Close();
        }
    });
}
