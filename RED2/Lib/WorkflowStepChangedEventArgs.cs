namespace RED2.Lib;

public class WorkflowStepChangedEventArgs : EventArgs
{

    public WorkflowStepChangedEventArgs(WorkflowSteps newStep)
    {
        NewStep = newStep;
    }

    public WorkflowSteps NewStep{get; set;}

}