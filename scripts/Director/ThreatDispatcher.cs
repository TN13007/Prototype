using Godot;


public partial class ThreatDispatcher : Node
{
    private EnemyManager manager;



    public override void _Ready()
    {
        Node found =
            GetTree()
            .GetFirstNodeInGroup("enemy_manager");

        if(found == null)
        {
            GD.PushError(
                "DISPATCHER: No node found in group " +
                "'enemy_manager'");

            return;
        }

        if(found is not EnemyManager enemyManager)
        {
            GD.PushError(
                "DISPATCHER: Group 'enemy_manager' must be " +
                "an EnemyManager");

            return;
        }

        manager = enemyManager;
    }



    public void Dispatch(
        ThreatRequest request)
    {
        if(request == null || manager == null ||
           !request.BeginDispatch())
            return;

        Enemy enemy =
            manager.FindBestCandidate(request);



        if(enemy == null)
        {
            request.Resolve(
                ThreatState.Failed,
                "No feasible actor");

            GD.Print("DISPATCHER: No feasible actor");
            return;
        }

        if(!manager.TryReserve(enemy, request) ||
           !enemy.ReceiveThreat(request))
        {
            enemy.ReleaseReservation(request);

            request.Resolve(
                ThreatState.Failed,
                "Actor reservation was rejected");
        }
    }
}
