using Godot;

public partial class Hud : CanvasLayer
{
	[ExportGroup("Hearts")]
	[Export] public Texture2D HeartFull { get; set; }
	[Export] public Texture2D HeartHalf { get; set; }
	[Export] public Texture2D HeartEmpty { get; set; }
 
	[ExportGroup("Rupee digits")]
	// Index 0 = the "0" glyph, index 9 = the "9" glyph.
	[Export] public Godot.Collections.Array<Texture2D> DigitTextures { get; set; } = new();

	private TextureRect _bIcon;
	private TextureRect _aIcon;
	private Control _hearts;
	private Control _rupeeDigits;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_bIcon = GetNode<TextureRect>("BottomBar/IconB");
		_aIcon = GetNode<TextureRect>("BottomBar/IconA");
		_hearts = GetNode<Control>("BottomBar/Hearts");
		_rupeeDigits = GetNode<Control>("BottomBar/RupeeDigits");

		SetItemB(null);
		SetItemA(null);
	}

	public void SetItemB(ItemData item) => _bIcon.Texture = item?.Icon;
	public void SetItemA(ItemData item) => _aIcon.Texture = item?.Icon;

	public void SetHearts(int currentHealth, int maxHealth)
	{
		int count = _hearts.GetChildCount();
		for (int i = 0; i < count; i++)
		{
			TextureRect heart = _hearts.GetChild<TextureRect>(i);
			heart.Visible = i * 2 < maxHealth;

			int remaining = currentHealth - i * 2;
			if (remaining >= 2)
				heart.Texture = HeartFull;
			else if (remaining == 1)
				heart.Texture = HeartHalf;
			else
				heart.Texture = HeartEmpty;
		}
	}

		public void SetRupees(int amount)
		{
			if (DigitTextures.Count < 10)
			{
				GD.PushWarning("HUD: assign all 10 DigitTextures (0-9).");
				return;
			}
 
			int digitCount = _rupeeDigits.GetChildCount();
			int maxValue = 1;
			for (int i = 0; i < digitCount; i++)
				maxValue *= 10;
			amount = Mathf.Clamp(amount, 0, maxValue - 1);
 
		// Fill from the ones place (last child) leftwards.
			for (int i = digitCount - 1; i >= 0; i--)
			{
				_rupeeDigits.GetChild<TextureRect>(i).Texture = DigitTextures[amount % 10];
				amount /= 10;
			}
		}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
