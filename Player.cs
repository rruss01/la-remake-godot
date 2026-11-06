using Godot;

// The class name must match the file name (Player.cs) and be 'partial' so Godot's source generators can attach to it.
public partial class Player : CharacterBody2D
{

	[Signal] public delegate void ItemBChangedEventHandler(ItemData item);
	[Signal] public delegate void ItemAChangedEventHandler(ItemData item);
	[Signal] public delegate void HealthChangedEventHandler(int health, int maxHealth);
	[Signal] public delegate void RupeesChangedEventHandler(int amount);

	private ItemData _itemB;
	private ItemData _itemA;
	private int _health;
	private int _rupees;

	public void TakeDamage(int amount) => SetHealth(_health - amount);
	public void Heal(int amount) => SetHealth(_health + amount);

	[Export] public ItemData StartingB { get; set; }
	[Export] public ItemData StartingA { get; set; }
	[Export] public int MaxHealth { get; set; } = 6;

	public void EquipB(ItemData item)
	{
		_itemB = item;
		EmitSignal(SignalName.ItemBChanged, item);
	}

	public void EquipA(ItemData item)
	{
		_itemA = item;
		EmitSignal(SignalName.ItemAChanged, item);
	}

	private void SetHealth(int value)
	{
		_health = Mathf.Clamp(value, 0, MaxHealth);
		EmitSignal(SignalName.HealthChanged, _health, MaxHealth);
	}

	public void AddRupees(int amount)
	{
		_rupees = Mathf.Clamp(_rupees + amount, 0, 999);
		EmitSignal(SignalName.RupeesChanged, _rupees);
	}

	// --- Movement (1/16 px per tick; values from the game's lookup table) ---
	private const int StraightSpeed = 16; // $10
	private const int DiagonalSpeed = 12; // $0C per axis
	private Vector2I _facing = Vector2I.Down;
	// Y starts at 8 so the axes don't pause on the same frame (fixes the diagonal hitch).
	private Vector2I _subpixel = new Vector2I(0, 8);

	// --- Sword ---
	// Arrays can't be 'const' in C#, so use static readonly.
	private static readonly int[] SwingSteps = [3, 3, 8]; // draw, swing start, swing middle
	private const int SwingTotal = 14; // sum of SwingSteps
	private const int SpinAtkMaxCharge = 40; // $28 frames to fully charge
	private int _swingFrame = -1; // -1 = not swinging
	private int _charge = 0;

	private AnimatedSprite2D _sprite;

	// dpad struct to handle movement
	private readonly record struct DPad(bool Right, bool Left, bool Up, bool Down)
	{
		public int X => B(Right) - B(Left);
		public int Y => B(Down) - B(Up);
		public int Count => B(Right) + B(Left) + B(Up) + B(Down);
		public bool Opposing => (Right && Left) || (Up && Down);

		private static int B(bool value) => value ? 1 : 0;
	}

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		GlobalPosition = GlobalPosition.Round();
		CallDeferred(MethodName.EquipStartingItems);
	}
	
	private void EquipStartingItems()
	{
		EquipB(StartingB);
		EquipA(StartingA);
		SetHealth(MaxHealth);
		AddRupees(0);
	}

	public override void _PhysicsProcess(double delta)
	{
		// 1. Swing in progress: movement fully blocked, only the swing runs
		if (_swingFrame >= 0)
		{
			UpdateSwing();
			return;
		}

		// 2. Button pressed: start a swing this tick (no movement this tick)
		if (Input.IsActionJustPressed("attack"))
		{
			StartSwing();
			return;
		}

		// 3. Charging (the game lets you walk while holding the sword out)
		if (Input.IsActionPressed("attack"))
		{
			_charge = Mathf.Min(_charge + 1, SpinAtkMaxCharge);
		}
		else
		{
			if (_charge >= SpinAtkMaxCharge)
			{
				// TODO: spin attack
			}
			_charge = 0;
		}

		// otherwise move normally
		UpdateMovement();
	}

	private DPad ReadDpad()
	{
		return new DPad(
			Right: Input.IsActionPressed("ui_right"),
			Left: Input.IsActionPressed("ui_left"),
			Up: Input.IsActionPressed("ui_up"),
			Down: Input.IsActionPressed("ui_down")
		);
	}

	// Facing only changes when exactly one direction is held (otherwise it's kept)
	private void UpdateFacing(DPad dpad)
	{
		if (dpad.Count == 1)
			_facing = new Vector2I(dpad.X, dpad.Y);
	}

	private string FacingDirection()
	{
		if (_facing.X != 0)
			return "right";
		return _facing.Y < 0 ? "up" : "down";
	}

	private void UpdateMovement()
	{
		DPad dpad = ReadDpad();

		// Speed in 1/16 px for this tick (opposing keys cancel everything)
		Vector2I speed = Vector2I.Zero;
		if (!dpad.Opposing)
		{
			int s = (dpad.X != 0 && dpad.Y != 0) ? DiagonalSpeed : StraightSpeed;
			speed = new Vector2I(dpad.X * s, dpad.Y * s);
		}

		// accumulate, then split into whole pixels + leftover fraction
		_subpixel += speed;
		Vector2I whole = new Vector2I(_subpixel.X >> 4, _subpixel.Y >> 4);
		_subpixel = new Vector2I(_subpixel.X & 15, _subpixel.Y & 15);

		// mimics LADX wall slide
		if (whole.X != 0)
			MoveAndCollide(new Vector2(whole.X, 0));
		if (whole.Y != 0)
			MoveAndCollide(new Vector2(0, whole.Y));

		UpdateFacing(dpad);

		// Walking animation = any d-pad direction held, even if blocked or cancelled
		string state = dpad.Count > 0 ? "walk" : "idle";
		_sprite.Play($"{state}-{FacingDirection()}");
		_sprite.FlipH = _facing.X < 0;
	}

	private void StartSwing()
	{
		UpdateFacing(ReadDpad());
		Velocity = Vector2.Zero;
		_charge = 0;
		_swingFrame = 0;
		UpdateSwing(); // show the first pose on this same tick, like the game
	}

	private void UpdateSwing()
	{
		int pose = 0;
		if (_swingFrame >= SwingSteps[0] + SwingSteps[1])
			pose = 2;
		else if (_swingFrame >= SwingSteps[0])
			pose = 1;

		_sprite.Animation = "attack-" + FacingDirection();
		_sprite.Frame = pose;
		_sprite.Pause();
		_sprite.FlipH = _facing.X < 0;

		_swingFrame++;
		if (_swingFrame >= SwingTotal)
			_swingFrame = -1;
	}
}
