/*
 * ------------------------------------- Strategy Camera Controller -----------------------------------------|
 * by: MarcWerk. October 2023                                                                                |
 * License: MIT                                                                                              |
 *																											 |
 * A basic customizable camera controller for use in games that uses a top-down perspective					 |
 * like strategy games or RPGs																				 |
 * ----------------------------------------------------------------------------------------------------------|
 */

using Godot;
using System;

public partial class StrategyCameraController : Node3D
{
	[Export] bool startActive = true;

	Node3D pitch;
	Node3D zoom;

	[Export] float moveSpeed = .25f;
	[Export] float turnSpeed = 120;
	[Export] float pitchSpeed = 1;
	[Export] Vector2 pitchRange = new Vector2(-75, -35);
	[Export] float zoomSpeed = 1;
	[Export] Vector2 zoomRange = new Vector2(5, 25);
	[Export] bool zoomAffectsMoveSpeed = true;
	[Export] Curve zoomToSpeedModifier = new Curve();
	[Export] float autoMoveSpeed = 7.5f;
	[Export] bool manualMoveCancelsAuto = true;
	[Export] bool edgeMove = true;
	[Export] float edgeWidth = .1f;
	[Export] float edgeMoveSpeedModifier = 2;
	[Export] bool edgePitch = true;
	[Export] float edgePitchDegrees = 200f;
	[Export] bool edgeRotate = true;

	[Export] string forwardAxis = "up";
	[Export] string leftAxis = "left";
	[Export] string backwardAxis = "down";
	[Export] string rightAxis = "right";

	[Export] string zoomInAxis = "num_plus";
	[Export] string zoomOutAxis = "num_minus";
	[Export] bool mouseScrollZoom = true;

	[Export] string rotateLeftAxis = "Q";
	[Export] string rotateRightAxis = "E";

	[Export] string pitchAxis = "ctrl_left";

	Vector3? moveTarget = null;
	Node3D followTarget = null;
	Vector2 ConvertedPitchRange() => new Vector2(Mathf.DegToRad(pitchRange.X), Mathf.DegToRad(pitchRange.Y));

	public bool ManualMoveCancelsAuto { get => manualMoveCancelsAuto; set => manualMoveCancelsAuto = value; }
	public bool Active { get; set; } = true;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Active = startActive;
		pitch = GetNode("Pitch") as Node3D;
		zoom = pitch.GetNode("Zoom") as Node3D;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(Active)
		{
			if (!Move(Input.GetVector(leftAxis, rightAxis, forwardAxis, backwardAxis) * (float)delta))
			{
				if (edgeMove)
				{
					var viewPort = GetViewport();
					Vector2 mPos = viewPort.GetMousePosition();
					var viewRect = viewPort.GetVisibleRect();

					bool ignoreX = edgeRotate && Input.GetActionStrength(pitchAxis) > .5f;
					bool ignoreY = edgePitch && Input.GetActionStrength(pitchAxis) > .5f;

					Vector2 move = Vector2.Zero;

					if (!ignoreX)
					{
						if (mPos.X >= 0 && mPos.X <= (viewRect.Size.X / 2) * edgeWidth)
							move.X = -1f;
						else if (mPos.X <= viewRect.Size.X && mPos.X >= viewRect.Size.X - ((viewRect.Size.X / 2) * edgeWidth))
							move.X = 1f;
					}

					if (!ignoreY)
					{
						if (mPos.Y >= 0 && mPos.Y <= (viewRect.Size.Y / 2) * edgeWidth)
							move.Y = -1f;
						else if (mPos.Y <= viewRect.Size.Y && mPos.Y >= viewRect.Size.Y - ((viewRect.Size.Y / 2) * edgeWidth))
							move.Y = 1f;
					}

					Move(move * edgeMoveSpeedModifier);
				}
			}
			
			if (moveTarget.HasValue)
			{
				Vector3 dir = moveTarget.Value - Position;
				dir.Y = 0;
				float step = autoMoveSpeed * (float)delta;
				if(dir.Length() <= step)
				{
					Position = new Vector3(moveTarget.Value.X, Position.Y, moveTarget.Value.Z);
					CancelMove();
				} else
				{
					Position = Position + (dir.Normalized() * step);
				}
			} else if (followTarget != null)
			{
				Vector3 dir = followTarget.Position - Position;
				dir.Y = 0;
				float step = autoMoveSpeed * (float)delta;
				if(dir.Length() <= step)
				{
					Position = new Vector3(followTarget.Position.X, Position.Y, followTarget.Position.Z);
				} else
				{
					Position = Position + (dir.Normalized() * step);
				}
			}

			Turn(Input.GetAxis(rotateLeftAxis, rotateRightAxis) * (float)delta);

			Zoom(Input.GetAxis(zoomInAxis, zoomOutAxis) * zoomSpeed);

			if (Input.GetActionStrength(pitchAxis) > .5f) {
				if (edgePitch)
				{
					var viewPort = GetViewport();
					Vector2 mPos = viewPort.GetMousePosition();
					var viewRect = viewPort.GetVisibleRect();

					if (mPos.Y >= 0 && mPos.Y <= (viewRect.Size.Y / 2) * edgeWidth)
						Pitch(-edgePitchDegrees * (float)delta);
					else if (mPos.Y <= viewRect.Size.Y && mPos.Y >= viewRect.Size.Y - ((viewRect.Size.Y / 2) * edgeWidth))
						Pitch( edgePitchDegrees * (float)delta);
				}

				if (edgeRotate)
				{
					var viewPort = GetViewport();
					Vector2 mPos = viewPort.GetMousePosition();
					var viewRect = viewPort.GetVisibleRect();

					if (mPos.X >= 0 && mPos.X <= (viewRect.Size.X / 2) * edgeWidth)
						Turn(-1f * (float)delta);
					else if (mPos.X <= viewRect.Size.X && mPos.X >= viewRect.Size.X - ((viewRect.Size.X / 2) * edgeWidth))
						Turn(1f * (float)delta);
				} 
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (Active)
		{
			if (!edgePitch && Input.GetActionStrength(pitchAxis) > 0.5f)
			{
				if (@event is InputEventMouseMotion motion)
				{
					Pitch(motion.Relative.Y);
				}
			}
			if (mouseScrollZoom && @event is InputEventMouseButton button)
			{
				if(button.IsPressed())
				{
					if(button.ButtonIndex == MouseButton.WheelDown)
						Zoom(-zoomSpeed);
					else if (button.ButtonIndex == MouseButton.WheelUp) 
						Zoom(zoomSpeed); 
				}
			}
		}
	}

	public void MoveTo (Vector3 moveTarget)
	{
		this.moveTarget = moveTarget;
	}

	public void CancelMove ()
	{
		moveTarget = null;
	}

	public void CancelFollow ()
	{
		followTarget = null;
	}

	bool Move (Vector2 delta)
	{
		if (delta == Vector2.Zero)
			return false;

		if (manualMoveCancelsAuto || !moveTarget.HasValue)
		{
			CancelFollow();
			CancelMove();
			Vector3 direction = (Transform.Basis * new Vector3(delta.X, 0, delta.Y)).Normalized();
			Position = Position + (direction * (moveSpeed * (zoomAffectsMoveSpeed ? ModifySpeedByZoomLevel() : 1)));
			return true;
		}

		return false;
	}

	float ModifySpeedByZoomLevel ()
	{
		float p = 1f / (zoomRange.Y - zoomRange.X) * (zoom.Position.Z - zoomRange.X);
		float mod = zoomToSpeedModifier.Sample(p);
		return mod;
	}

	void Turn (float degreesDelta)
	{
		RotateY(Mathf.DegToRad(degreesDelta * turnSpeed));
	}

	void Pitch (float degreesDelta)
	{
		var range = ConvertedPitchRange();
		pitch.Rotation = new Vector3(Mathf.Clamp(pitch.Rotation.X + Mathf.DegToRad(degreesDelta * pitchSpeed), range.X, range.Y), pitch.Rotation.Y, pitch.Rotation.Z);
	}

	void Zoom (float delta)
	{
		zoom.Position = new Vector3(zoom.Position.X, zoom.Position.Y, Mathf.Clamp(zoom.Position.Z + delta, zoomRange.X, zoomRange.Y));
	}
}
