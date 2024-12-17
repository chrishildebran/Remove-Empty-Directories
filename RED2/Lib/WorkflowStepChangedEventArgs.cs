namespace RED2.Lib;

using System;

public class WorkflowStepChangedEventArgs : EventArgs
{

    public WorkflowStepChangedEventArgs(WorkflowSteps newStep)
    {
        this.NewStep = newStep;
    }

    public WorkflowSteps NewStep{get; set;}

}