using System.Diagnostics;
using ConsoleApp1.StateMachineTest.Enums;
using Stateless;

namespace ConsoleApp1.StateMachineTest;

public class InspectionStateMachine
{
    private bool Initialized { get; set; }
    private StateMachine<InspectionState, OperationTrigger> StateMachine { get; } = new(InspectionState.Stop);

    public void InitializeStateMachine()
    {
        Debug.Assert(!Initialized);
        ConfigureStateMachine(StateMachine);
        Initialized = true;
        return;

        void ConfigureStateMachine(StateMachine<InspectionState, OperationTrigger> stateMachine)
        {
            stateMachine.Configure(InspectionState.Stop)
                .Permit(OperationTrigger.Scan, InspectionState.Prepared);

            stateMachine.Configure(InspectionState.Prepared)
                .Permit(OperationTrigger.Inspect, InspectionState.Inspecting)
                .OnEntryAsync(PrepareInspectAsync)
                .OnEntryAsync(async () => await stateMachine.FireAsync(OperationTrigger.Inspect));

            stateMachine.Configure(InspectionState.Inspecting)
                .Permit(OperationTrigger.Review, InspectionState.Reviewing)
                .OnEntryAsync(InspectAsync)
                .OnEntryAsync(async () => await stateMachine.FireAsync(OperationTrigger.Review));

            stateMachine.Configure(InspectionState.Reviewing)
                .Permit(OperationTrigger.Complete, InspectionState.End)
                .OnEntryAsync(ReviewAsync)
                .OnEntryAsync(async () => await stateMachine.FireAsync(OperationTrigger.Complete));

            stateMachine.Configure(InspectionState.End)
                .OnEntryAsync(CompletedAsync);
            
            stateMachine.OnUnhandledTriggerAsync(ProcessUnhandledTriggerAsync);
        }
    }

    public async Task StartAsync()
    {
        Debug.Assert(Initialized);
        await StateMachine.FireAsync(OperationTrigger.Scan);
    }

    private async Task PrepareInspectAsync()
    {
        await Task.Delay(0);
        Console.WriteLine("Preparing");
        await StateMachine.FireAsync(OperationTrigger.Abort);
    }

    private async Task InspectAsync()
    {
        await Task.Delay(0);
        Console.WriteLine("Inspecting");
    }

    private async Task ReviewAsync()
    {
        await Task.Delay(0);
        Console.WriteLine("Reviewing");
    }
    
    private async Task CompletedAsync()
    {
        await Task.Delay(0);
        Console.WriteLine("Completed");
    }
    
    private async Task ProcessUnhandledTriggerAsync(InspectionState arg1, OperationTrigger arg2, ICollection<string> arg3)
    {
        await Task.Delay(0);
        Console.WriteLine("Processing unhandled trigger");
    }
}