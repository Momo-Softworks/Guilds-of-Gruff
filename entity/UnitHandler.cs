using Godot;
using System.Collections.Generic;

public partial class UnitHandler : CharacterBody3D
{
    const float MOVE_SPEED = 10;
    
    public static Dictionary<Vector3, HashSet<UnitHandler>> DESTINATIONS = new Dictionary<Vector3, HashSet<UnitHandler>>();
    public static Dictionary<Vector3, Node3D> PATH_POINTERS = new Dictionary<Vector3, Node3D>();
    
    private bool selected = false;
    private NavigationRegion3D terrainNav;
	private NavigationAgent3D navigator;
	
	public override void _Ready()
	{
		navigator = GetNode<NavigationAgent3D>("Navigator");
	}

	public override void _PhysicsProcess(double delta)
	{
		// Pathfinding
		if (!navigator.IsTargetReached())
		{
			Vector3 selfPos = GlobalTransform.Origin;
			Vector3 nextPos = navigator.GetNextPathPosition();
			float distance = selfPos.DistanceTo(nextPos);
			
			if (distance > navigator.GetPathDesiredDistance())
			{   navigator.Velocity = nextPos - selfPos;
			}
		}
        else
        {
            navigator.Velocity = new Vector3();
            if (DESTINATIONS.ContainsKey(navigator.GetTargetPosition()))
            {
                DESTINATIONS[navigator.GetTargetPosition()].Remove(this);
                getSelection().updatePathDestinations();
            }
        }

		// Movement
		Velocity += (navigator.Velocity.Normalized() * MOVE_SPEED - Velocity) * 0.5f;
		// Friction
		Velocity *= new Vector3(0.85f, 1, 0.85f);
		// Gravity
		Velocity += new Vector3(0, -9.8f, 0);
		MoveAndSlide();
		base._PhysicsProcess(delta);
	}

	public void setSelected(bool selected)
	{
		this.selected = selected;
		(FindChild("Selector") as Node3D).SetVisible(selected);
	}
	
	public bool isSelected()
	{   return selected;
	}
	
	public void pathfindTo(Vector3 destination)
	{   
		navigator.SetTargetPosition(destination);
		if (!navigator.IsTargetReachable())
		{   navigator.SetTargetPosition(Position);
		}
        else
        {
            if (!DESTINATIONS.ContainsKey(destination))
            {
                DESTINATIONS[destination] = new HashSet<UnitHandler>();
                getSelection().addPathDestination(destination);
            }
            DESTINATIONS[destination].Add(this);
        }
	}
    
    private Selection getSelection()
    {
        return GetTree().Root.GetNode<Selection>("World/SelectionBox");
    }
}
