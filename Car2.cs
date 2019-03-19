using Godot;
using System;

public class Car2 : KinematicBody2D
{
    public Vector2 velocity;
    private bool sensorState;

    private VisibilityNotifier2D _visibilityNotifier;

    [Signal]
    delegate void carSensor(int carId, bool state);

    public override void _Ready()
    {
        _visibilityNotifier = GetNode<VisibilityNotifier2D>("CarSprite2/VisibilityNotifier2D");

        velocity = new Vector2();
        velocity.x += 3;
    }

    public override void _PhysicsProcess(float delta)
    {
        KinematicCollision2D collision = MoveAndCollide(velocity);
        if (collision != null &&
            (collision.Collider as CollisionObject2D).Name == "Crossing")
        {
            if (!sensorState)
            {
                EmitSignal(nameof(carSensor), 2, true);
                sensorState = true;
            }
        }
        else if (sensorState)
        {
            EmitSignal(nameof(carSensor), 2, false);
            sensorState = false;
        }
    }

    public override void _Process(float delta)
    {
        if (!_visibilityNotifier.IsOnScreen())
        {
            SetPosition(new Vector2(-100, 597));
        }
    }

    public void Move()
    {
        SetCollisionLayerBit(0, false);
        SetCollisionMaskBit(0, false);

        SetCollisionLayerBit(1, true);
        SetCollisionMaskBit(1, true);
    }

    public void Stop()
    {
        SetCollisionLayerBit(0, true);
        SetCollisionMaskBit(0, true);

        SetCollisionLayerBit(1, false);
        SetCollisionMaskBit(1, false);
    }
}
