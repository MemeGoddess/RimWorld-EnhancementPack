using RimWorld;

namespace TD_Enhancement_Pack
{
	public class TransferableComparer_ValuePerFullStack : TransferableComparer
	{
		public override int Compare(Transferable lhs, Transferable rhs)
		{
			return (lhs.AnyThing.MarketValue * lhs.ThingDef.stackLimit)
				.CompareTo(rhs.AnyThing.MarketValue * rhs.ThingDef.stackLimit);
		}
	}
}
