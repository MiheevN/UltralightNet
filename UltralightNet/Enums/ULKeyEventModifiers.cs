namespace UltralightNet
{
	/// <summary>An enumeration of the different keyboard modifiers.</summary>
	public enum ULKeyEventModifiers: byte
	{
		/// <summary>Whether or not an ALT key is down</summary>
		AltKey = 1 << 0,
		/// <summary>Whether or not a Control key is down</summary>
		CtrlKey = 1 << 1,
		/// <summary>Whether or not a meta key (Command-key on Mac, Windows-key on Win) is down</summary>
		MetaKey = 1 << 2,
		/// <summary>Whether or not a Shift key is down</summary>
		ShiftKey = 1 << 3,
	}
}
