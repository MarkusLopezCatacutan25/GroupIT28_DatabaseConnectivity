using System;
using System.Threading.Tasks;

public static class SignInWorkflowTests
{
    private sealed class Identity : IChefIdentity
    {
        public string Current;
        public int Calls;
        public TaskCompletionSource<string> Completion=new TaskCompletionSource<string>();
        public string Uid=>Current;
        public Task<string> SignInAsync(){Calls++;return Completion.Task;}
    }
    private static int count;
    private static void Check(bool value,string label){if(!value)throw new Exception(label);count++;Console.WriteLine("SIGN-IN PASS "+label);}
    private static async Task Started(Identity identity){for(int i=0;i<100&&identity.Calls==0;i++)await Task.Delay(1);}
    public static async Task Run()
    {
        var identity=new Identity();var flow=new ChefSignInCoordinator(identity);
        Check(!flow.CanEnter,"recipe access is locked before explicit sign-in");
        Task pending=flow.SignInAsync("ChefMaria");Check(flow.Busy&&!flow.CanEnter,"sign-in exposes Loading before awaiting authentication");await Started(identity);
        await flow.SignInAsync("OtherChef");Check(identity.Calls==1,"duplicate submit cannot start another authentication request");
        identity.Current="real-uid";identity.Completion.SetResult("real-uid");await pending;
        Check(flow.CanEnter&&flow.Uid=="real-uid"&&flow.DisplayName=="ChefMaria","only confirmed UID permits entry; display name stays separate");
        identity.Current="other-uid";Check(!flow.CanEnter,"a changed account invalidates the access decision");
        identity=new Identity();flow=new ChefSignInCoordinator(identity);await flow.SignInAsync("Chef\nMaria");Check(flow.State==ChefSignInState.Error&&identity.Calls==0,"invalid display text cannot trigger authentication");
        pending=flow.SignInAsync("");await Started(identity);identity.Current="guest-uid";identity.Completion.SetResult("guest-uid");await pending;Check(flow.CanEnter&&flow.DisplayName=="Chef","optional empty name becomes Chef, never an identity");
        identity=new Identity();flow=new ChefSignInCoordinator(identity);pending=flow.SignInAsync("Chef");await Started(identity);identity.Completion.SetException(new Exception("Network unavailable"));await pending;Check(flow.State==ChefSignInState.Error&&!flow.CanEnter&&flow.Message.Contains("retry"),"authentication failure keeps entry locked and offers retry");
        identity.Completion=new TaskCompletionSource<string>();pending=flow.SignInAsync("Chef");identity.Current="recovered-uid";identity.Completion.SetResult(identity.Current);await pending;Check(flow.CanEnter,"retry can recover after a sign-in error");
        identity=new Identity();flow=new ChefSignInCoordinator(identity,20);pending=flow.SignInAsync("Chef");await Started(identity);await pending;identity.Current="late-uid";identity.Completion.SetResult(identity.Current);await Task.Delay(5);Check(flow.State==ChefSignInState.Error&&!flow.CanEnter,"late authentication after timeout cannot open a scene");
        identity=new Identity();flow=new ChefSignInCoordinator(identity);pending=flow.SignInAsync("Chef");await Started(identity);flow.Cancel();identity.Current="cancelled-uid";identity.Completion.SetResult(identity.Current);await pending;Check(flow.State==ChefSignInState.Idle&&!flow.CanEnter,"Back cancels navigation even if native authentication later completes");
        identity=new Identity();flow=new ChefSignInCoordinator(identity);pending=flow.SignInAsync("Chef");await Started(identity);flow.Dispose();identity.Current="disposed-uid";identity.Completion.SetResult(identity.Current);await pending;Check(!flow.CanEnter,"destroyed sign-in screen ignores late authentication");
        identity=new Identity();flow=new ChefSignInCoordinator(identity);pending=flow.SignInAsync("Chef");identity.Completion.SetResult(null);await pending;Check(!flow.CanEnter&&flow.State==ChefSignInState.Error,"an empty authentication result is rejected");
        identity=new Identity{Current="uid-b"};flow=new ChefSignInCoordinator(identity);pending=flow.SignInAsync("Chef");identity.Completion.SetResult("uid-a");await pending;Check(!flow.CanEnter&&flow.State==ChefSignInState.Error,"returned UID must match the SDK's current identity");
        Console.WriteLine("COMPLETE "+count+" sign-in workflow checks passed; test identity gateways only.");
    }
}
