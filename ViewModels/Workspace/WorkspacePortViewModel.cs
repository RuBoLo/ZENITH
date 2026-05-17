namespace ZENITH.ViewModels.Workspace;

public sealed class WorkspacePortViewModel(
    string id,
    string name,
    string dataType,
    WorkspacePortDirection direction) : ViewModelBase
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string DataType { get; } = dataType;
    public WorkspacePortDirection Direction { get; } = direction;

    public static WorkspacePortViewModel Input(string id, string name, string dataType)
        => new(id, name, dataType, WorkspacePortDirection.Input);

    public static WorkspacePortViewModel Output(string id, string name, string dataType)
        => new(id, name, dataType, WorkspacePortDirection.Output);
}
