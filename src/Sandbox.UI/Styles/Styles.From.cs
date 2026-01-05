namespace Sandbox.UI
{
	public partial class Styles
	{
		public override void Add( BaseStyles bs )
		{
			base.Add( bs );

			if ( bs is not Styles a )
				return;

			if ( a.HasTransitions )
			{
				if ( Transitions == null )
					Transitions = new TransitionList();

				Transitions.AddTransitions( a.Transitions );
			}

			BoxShadow.AddFrom( a.BoxShadow );
			TextShadow.AddFrom( a.TextShadow );
			FilterDropShadow.AddFrom( a.FilterDropShadow );

			// Merge CSS custom properties (variables starting with --)
			foreach ( var kvp in a.RawValues )
			{
				RawValues[kvp.Key] = kvp.Value;
			}

			if ( a.TextGradient.HasValue )
			{
				TextGradient = a.TextGradient;
			}
		}

		public override void From( BaseStyles bs )
		{
			base.From( bs );

			if ( bs is not Styles a ) return;

			CopyShadows( bs );

			// Copy CSS custom properties (variables starting with --)
			RawValues.Clear();
			foreach ( var kvp in a.RawValues )
			{
				RawValues[kvp.Key] = kvp.Value;
			}

			Transitions?.Clear();

			if ( a.HasTransitions )
			{
				Transitions ??= new TransitionList();

				Transitions.AddTransitions( a.Transitions );
			}

			if ( a.TextGradient.HasValue )
			{
				TextGradient = a.TextGradient;
			}

		}

		internal void CopyShadows( BaseStyles bs )
		{
			if ( bs is not Styles a )
				return;

			BoxShadow.Clear();
			BoxShadow.IsNone = a.BoxShadow.IsNone;
			BoxShadow.AddRange( a.BoxShadow );

			TextShadow.Clear();
			TextShadow.IsNone = a.TextShadow.IsNone;
			TextShadow.AddRange( a.TextShadow );

			FilterDropShadow.Clear();
			FilterDropShadow.IsNone = a.FilterDropShadow.IsNone;
			FilterDropShadow.AddRange( a.FilterDropShadow );
		}
	}
}
