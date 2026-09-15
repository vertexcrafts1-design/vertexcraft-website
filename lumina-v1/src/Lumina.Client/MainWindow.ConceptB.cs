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
        }
        finally
        {
            _syncingInstanceSelection = false;
        }
    }
}
