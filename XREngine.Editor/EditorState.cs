namespace XREngine.Editor;
public class EditorState
{
    public enum State
    {
        Play,
        Edit,
        EnteringPlay,
        ExitingPlay,
        EnteringEdit,
        ExitingEdit,
    }

    public static State CurrentState { get; set; } = State.Edit;
    public static bool InEditMode => CurrentState == State.Edit || CurrentState == State.EnteringEdit || CurrentState == State.ExitingEdit;
    public static bool InPlayMode => CurrentState == State.Play || CurrentState == State.EnteringPlay || CurrentState == State.ExitingPlay;
}