using Godot;
using System;

public partial class MainMenu : Control
{
	public const string SINGLEPLAYER_PATH = "res://world/game_world.tscn";
	
	public void singleplayerPressed()
	{
		GetTree().ChangeSceneToFile(SINGLEPLAYER_PATH);
	}
}
