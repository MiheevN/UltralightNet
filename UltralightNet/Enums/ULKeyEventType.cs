namespace UltralightNet
{
	/// <summary>
	/// type of <see cref="ULKeyEvent"/>
	/// </summary>
	public enum ULKeyEventType
	{
		/// <summary>
		/// Key-Down event type. (Does not trigger accelerator commands in WebCore)
		/// </summary>
		/// <remarks>
		/// You should probably use RawKeyDown instead when a physical key is pressed. This member is only here for historic compatibility with WebCore's key event types.
		/// </remarks>
		KeyDown,
		/// <summary>
		/// Key-Up event type. Use this when a physical key is released.
		/// </summary>
		KeyUp,
		/// <summary>
		/// Raw Key-Down type. Use this when a physical key is pressed.
		/// </summary>
		RawKeyDown,
		/// <summary>
		/// Character input event type. Use this when the OS generates text from a physical key being pressed (eg, WM_CHAR on Windows).
		/// </summary>
		Char
	}
}
