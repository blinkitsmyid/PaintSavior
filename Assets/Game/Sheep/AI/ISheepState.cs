namespace Game.Sheep.AI
{
    internal interface ISheepState
    {
        SheepStateId Id { get; }

        void Enter();

        void Tick(float deltaTime);

        void Exit();
    }
}
