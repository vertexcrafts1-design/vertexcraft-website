using System.Windows;
using System.Windows.Controls;
using Lumina.Core;

namespace Lumina.Client;

public partial class MainWindow
{
    private bool _conceptBReady;
    private bool _syncingInstanceSelection;

    private void ConceptB_Loaded(object sender, RoutedEventArgs e)
    {
        if (_conceptBReady) return;
        _conceptBReady = true;

        ActiveInstanceCombo.ItemsSource = _instances;
        ActiveInstanceCombo.SelectedItem = InstancesList.SelectedItem;
        InstancesList.SelectionChanged += ConceptBInstances_SelectionChanged;
        AddHandler(Button.ClickEvent, new RoutedEventHandler(ConceptB_ButtonClick));
        _discoverStorefront?.SetInstanceContext(SelectedInstance);
    }

    private void ConceptB_ButtonClick(object sender, RoutedEventArgs e)
    {
        if (e.Source is not Button { Tag: string tag } || !tag.StartsWith("ram:", StringComparison.OrdinalIgnoreCase))
            return;

        if (!int.TryParse(tag.AsSpan(4), out var ramMb)) return;
        RamSlider.Value = Math.Clamp(ramMb, (int)RamSlider.Minimum, (int)RamSlider.Maximum);
        e.Handled = true;
    }

    private void ActiveInstanceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || _syncingInstanceSelection || ActiveInstanceCombo.SelectedItem is not InstanceProfile profile)
            return;

        try
        {
            _syncingInstanceSelection = true;
            InstancesList.SelectedItem = profile;
            InstancesList.ScrollIntoView(profile);
        }
        finally
        {
            _syncingInstanceSelection = false;
        }
    }

    private void ConceptBInstances_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingInstanceSelection || ActiveInstanceCombo is null) return;
        try
        {
            _syncingInstanceSelection = true;
            ActiveInstanceCombo.SelectedItem = InstancesList.SelectedItem;
            _discoverStorefront?.SetInstanceContext(SelectedInstance);
        }
        finally
        {
            _syncingInstanceSelection = false;
        }
    }
}
