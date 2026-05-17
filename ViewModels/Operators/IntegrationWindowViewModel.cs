using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ZENITH.ViewModels.Workspace;

namespace ZENITH.ViewModels.Operators;

public partial class IntegrationWindowViewModel : WorkspaceItemViewModel
{
    public IntegrationWindowViewModel(double x, double y)
        : base("Integration", x, y, 360, 460)
    {
        Frames = [];
        StackingAlgorithms = ["Average", "Median", "Sigma Clipping"];
        RejectionAlgorithms = ["None", "Min/Max", "Winsorized Sigma"];
        SelectedStackingAlgorithm = StackingAlgorithms[0];
        SelectedRejectionAlgorithm = RejectionAlgorithms[0];

        Ports.Add(WorkspacePortViewModel.Input("frames", "Frames", "Image[]"));
        Ports.Add(WorkspacePortViewModel.Output("integrated-image", "Integrated image", "Image"));

        ExecuteCommand = new RelayCommand(Execute);
        HaltCommand = new RelayCommand(Halt);
    }

    public ObservableCollection<string> Frames { get; }
    public string[] StackingAlgorithms { get; }
    public string[] RejectionAlgorithms { get; }
    public IRelayCommand ExecuteCommand { get; }
    public IRelayCommand HaltCommand { get; }

    public void AddFramePaths(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            if (!Frames.Contains(path))
                Frames.Add(path);
        }
    }

    [ObservableProperty]
    private string? _selectedStackingAlgorithm;

    [ObservableProperty]
    private string? _selectedRejectionAlgorithm;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private double _progress;

    private void Execute()
    {
        Status = "Ready";
    }

    private void Halt()
    {
        Status = "Idle";
        Progress = 0;
    }
}