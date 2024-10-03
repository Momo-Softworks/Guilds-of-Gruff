using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Selection : Control
{
	private const float PANNING_SPEED = 0.65f;
	private static Vector2 PANNING_ORIGIN = new Vector2();
	
	private static bool LEFT_DRAGGING = false;
	private static bool MIDDLE_DRAGGING = false;
	private static Vector2 LEFT_DRAG_START = new Vector2();
	private static Vector2 MIDDLE_DRAG_START = new Vector2();
	
	private static List<UnitHandler> SELECTED_UNITS = new List<UnitHandler>();

	public override void _PhysicsProcess(double delta)
	{ 
		bool middleClicking = Input.IsMouseButtonPressed(MouseButton.Middle);
		Vector2 mousePos = GetViewport().GetMousePosition();
		if (middleClicking)
		{   handleCameraManualPanning();
		}
		else
		{
			MIDDLE_DRAGGING = false;
			if (Input.IsActionPressed("panning_mode"))
			{   
				if (PANNING_ORIGIN.Equals(Vector2.Zero))
				{   PANNING_ORIGIN = mousePos;
				}
				handleCameraEdgePanning();
			}
			else PANNING_ORIGIN = Vector2.Zero;
		}
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseButton)
		{
			switch (mouseButton.ButtonIndex)
			{
				case MouseButton.Left:
				{   // Left-click for dragging
					handleLeftClick(mouseButton);
					break;
				}
				case MouseButton.Right:
				{   // Right-click for moving units
					handleRightClick(mouseButton);
					break;
				}
			}
		}
	}
	
	public override void _Draw()
	{
		// Draw selection box
		if (LEFT_DRAGGING)
		{
			Rect2 dragRect = new Rect2(LEFT_DRAG_START, GetViewport().GetMousePosition() - LEFT_DRAG_START);
			DrawRect(dragRect, new Color(0.8f, 0.8f, 1, 0.3f));
			DrawRect(dragRect, new Color(0.8f, 0.8f, 1, 0.8f), false, 3f);
		}

		if (!PANNING_ORIGIN.Equals(Vector2.Zero))
		{
			Vector2 relative = GetViewport().GetMousePosition() - PANNING_ORIGIN;
			float distance = relative.Length();
			float radius = Math.Min(100, distance / 4f);
			DrawCircle(PANNING_ORIGIN, radius, new Color(0.8f, 0.8f, 1, 0.3f));
			// draw a line between the edge of the circle (not directly on the origin) and the mouse
			DrawLine(PANNING_ORIGIN + relative.Normalized() * radius, GetViewport().GetMousePosition(), new Color(0.8f, 0.8f, 1, 1f), 3);
		}
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	private void handleCameraEdgePanning()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Camera3D camera = GetViewport().GetCamera3D();
		Window window = GetWindow();
		float mouseX = Math.Clamp(((mousePos.X - PANNING_ORIGIN.X) / window.Size.X) * 4, -PANNING_SPEED, PANNING_SPEED);
		float mouseY = Math.Clamp(((mousePos.Y - PANNING_ORIGIN.Y) / window.Size.Y) * 4, -PANNING_SPEED, PANNING_SPEED);
		
		camera.GlobalTranslate(new Vector3(mouseX + mouseY, 0, -mouseX + mouseY));
	}

	private void handleCameraManualPanning()
	{
		Vector2 mousePos = GetViewport().GetMousePosition();
		Camera3D camera = GetViewport().GetCamera3D();
		// click and drag to move camera
		if (!MIDDLE_DRAGGING)
		{   MIDDLE_DRAG_START = mousePos;
			MIDDLE_DRAGGING = true;
		}
		else
		{
			Vector2 drag = MIDDLE_DRAG_START - mousePos;
			Vector3 drag3D = new Vector3(drag.X / 41.5f, 0, drag.Y / 29.5f)
				.Rotated(new Vector3(1, 0, 0), Mathf.DegToRad(45));
			camera.Translate(drag3D);
			MIDDLE_DRAG_START = mousePos;
		}
	}
	
	private void handleLeftClick(InputEventMouseButton mouseButton)
	{
		// Initiate drag
		if (mouseButton.Pressed)
		{   
			// Unselect units
			foreach (UnitHandler unit in SELECTED_UNITS)
			{   unit.setSelected(false);
			}
			SELECTED_UNITS.Clear();
						
			// Start drag
			LEFT_DRAG_START = mouseButton.Position;
			LEFT_DRAGGING = true;
		}
		// End drag
		else if (LEFT_DRAGGING)
		{  
			LEFT_DRAGGING = false;
		
			// Create selection box
			var selection = new Rect2(LEFT_DRAG_START, GetViewport().GetMousePosition() - LEFT_DRAG_START);
			// If selection is small (single-click), select unit under mouse
			if (selection.Size.Length() < 5)
			{
				Node3D selected = getObjectUnderMouse(2);
				if (selected is UnitHandler unit)
				{
					SELECTED_UNITS.Add(unit);
					unit.setSelected(true);
				}
			}
			// If selection is large, select all units in selection box
			else
			{   // Add units
				List<Node3D> selected = getAllInSelection(selection);
				foreach (Node3D obj in selected)
				{
					if (obj is UnitHandler unit)
					{   SELECTED_UNITS.Add(unit);
					}
				}
				// Mark units as selected
				foreach (Node3D obj in selected)
				{
					if (obj is UnitHandler unit)
					{   unit.setSelected(true);
					}
				}
			}
		}
	}
	
	private void handleRightClick(InputEventMouseButton mouseButton)
	{
		// Move selected units
		if (mouseButton.Pressed)
		{
			// Get destination
			Vector3 destination = getClickedPos(1);
			// Calculate path
			foreach (UnitHandler unit in SELECTED_UNITS)
			{
				unit.pathfindTo(destination);
			}
		}
	}
	
	public Node3D getObjectUnderMouse(uint layer = 4294967295)
	{
		Viewport viewport = GetViewport();
		Camera3D camera = viewport.GetCamera3D();
		Vector2 mousePos = viewport.GetMousePosition();
		Vector3 rayFrom = camera.ProjectRayOrigin(mousePos);
		Vector3 ray = camera.ProjectRayNormal(mousePos);
		Vector3 rayTo = rayFrom + ray * 1000;
		PhysicsDirectSpaceState3D spaceState = camera.GetWorld3D().GetDirectSpaceState();
		Godot.Collections.Dictionary result = spaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(rayFrom, rayTo, layer));

		if (result != null && result.ContainsKey("collider"))
		{   return ((GodotObject) result["collider"] as CollisionObject3D);
		}
		return null;
	}

	public Vector3 getClickedPos(uint layer = 4294967295)
	{
		Viewport viewport = GetViewport();
		Camera3D camera = viewport.GetCamera3D();
		Vector2 mousePos = viewport.GetMousePosition();
		Vector3 rayFrom = camera.ProjectRayOrigin(mousePos);
		Vector3 rayTo = rayFrom + camera.ProjectRayNormal(mousePos) * 1000;
		
		PhysicsDirectSpaceState3D spaceState = viewport.World3D.DirectSpaceState;
		Godot.Collections.Dictionary result = spaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(rayFrom, rayTo, layer));
		
		if (result != null && result.ContainsKey("position"))
		{   return (Vector3) result["position"];
		}
		return Vector3.Zero;
	}
	
	public List<Node3D> getAllInSelection(Rect2 selectionBox)
	{
		selectionBox = fixRectangle(selectionBox);
		List<Node3D> selected = new List<Node3D>();
		foreach (Node node in GetTree().GetNodesInGroup("selectable"))
		{   
			if (node is Node3D node3D)
			{
				Vector2 screenPos = node3D.GetViewport().GetCamera3D().UnprojectPosition(node3D.GlobalTransform.Origin);
				if (selectionBox.HasPoint(screenPos))
				{   selected.Add(node3D);
				}
			}
		}
		return selected;
	}

	private Rect2 fixRectangle(Rect2 rectangle)
	{
		// Fix negative width/height
		if (rectangle.Size.X < 0)
		{
			rectangle.Position += new Vector2(rectangle.Size.X, 0);
			rectangle.Size = new Vector2(-rectangle.Size.X, rectangle.Size.Y);
		}
		if (rectangle.Size.Y < 0)
		{
			rectangle.Position += new Vector2(0, rectangle.Size.Y);
			rectangle.Size = new Vector2(rectangle.Size.X, -rectangle.Size.Y);
		}
		return rectangle;
	}

	public void addPathDestination(Vector3 pos)
	{
		PackedScene pointer = GD.Load<PackedScene>("res://effect/PathPointer.tscn");
		Node3D pathPointer = pointer.Instantiate() as Node3D;
		pathPointer.GlobalTransform = new Transform3D(Basis.Identity, pos);
		pathPointer.Scale = new Vector3(2f, 2f, 2f);
		GetTree().Root.AddChild(pathPointer);
		UnitHandler.PATH_POINTERS[pos] = pathPointer;
	}

	public void updatePathDestinations()
	{
		for (var i = UnitHandler.PATH_POINTERS.Values.Count - 1; i >= 0; i--)
		{
			Vector3 pointerPos = UnitHandler.PATH_POINTERS.Keys.ElementAt(i);
			if (!UnitHandler.DESTINATIONS.ContainsKey(pointerPos) || UnitHandler.DESTINATIONS[pointerPos].Count == 0)
			{
				HashSet<UnitHandler> units = UnitHandler.DESTINATIONS[pointerPos];
				Vector3 pos = UnitHandler.DESTINATIONS.FirstOrDefault(x => x.Value == units).Key;
				if (UnitHandler.PATH_POINTERS.ContainsKey(pos))
				{
					UnitHandler.PATH_POINTERS[pos].QueueFree();
					UnitHandler.PATH_POINTERS.Remove(pos);
				}
				UnitHandler.DESTINATIONS.Remove(pos);
				i--;
			}
		}
	}
}
