using Godot;
using System;

public partial class MainMenu : Control
{
	public const string GAME_PATH = "res://world/game_world.tscn";
	public const string MULTIPLAYER_SCREEN_PATH = "res://menu/multiplayer_menu.tscn";
	
	public void singleplayerPressed()
	{
		GetTree().ChangeSceneToFile(GAME_PATH);
	}
	
	public void multiplayerPressed()
	{
		GetTree().ChangeSceneToFile(MULTIPLAYER_SCREEN_PATH);
	}
}
