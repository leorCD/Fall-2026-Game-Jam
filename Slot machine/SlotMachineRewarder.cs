using Godot;
using System;
using System.Numerics;

public readonly struct SpinOutcome
{
    public BigInteger Payout {get;}
    public bool IsWin => Payout > 0;
    public string Description {get;}   // for UI / debug, e.g. "Triple 7" or "Pair"
    public bool IsJackpot {get;}

    public SpinOutcome(BigInteger payout, string description, bool isJackpot = false)
    {
        Payout = payout;
        Description = description;
        IsJackpot = isJackpot;
    }
}

public partial class SlotMachineRewarder : Node
{
	private StaticPlayerController Player;

    // multiplier of the bet per face for a triple. Keys match your face numbers.
    [Export] public Godot.Collections.Dictionary<int, double> TripleMultipliers = new()
    {
        { 1, 100.5 }, // ie. a full row of Face1 gives you 3x the input
		{ 2, 300 },
		{ 3, 600 },
		{ 4, 1000 },
        { 5, 1400 },
		{ 6, 1800 },
		{ 7, 2100 },
		{ 8, 2500 },
    };
	
    [Export] public double PairMultiplierFraction = 0.25; // fraction of the triple multiplier
    [Export] public double MinimumPairMultiplier = 1.25; // guaranteed floor so rolling a pair of Face1 always slightly profits
    [Export] public double AdjacentPairMultiplier = 1.25; // 2 in a row only gives you 1.25x of what you initially bet

	[ExportCategory("DEBUG")]
	[Export] public bool printOutcome = true;










	public void init(SlotMachineLogic Logic)
	{
		if (Logic.Player == null)
			GD.PrintErr($"{nameof(SlotMachineRewarder)} has an empty player object, nothing can be rewarded");

		this.Player = Logic.Player;
	}

    public SpinOutcome Evaluate(int[] results, BigInteger bet)
    {
		// all 3 faces are the same
        if (results.Length >= 3 && results[0] == results[1] && results[1] == results[2])
        {
            double mult = TripleMultipliers.TryGetValue(results[0], out double m) ? m : 1.0;
			BigInteger payout = MultiplyBigInt(bet, mult);

            return new SpinOutcome(
				payout,
				$"Triple {results[0]}",
				true
			);
        }

		int matchedFace = -1; // find the matched face for pair multiplier (pairs have to be adjacent)
		if (results[0] == results[1])
			matchedFace = results[0];
		else if (results[1] == results[2])
			matchedFace = results[1];
		
		// only 2 faces are the same
		if (matchedFace != -1)
		{
			double tripleMult = TripleMultipliers.TryGetValue(matchedFace, out double m) ? m : 1.0;
			double pairMult = Math.Max(tripleMult * PairMultiplierFraction, MinimumPairMultiplier); // scaled down from the jackpot table, but kept at or above the minimum floor
			BigInteger payout = MultiplyBigInt(bet, pairMult);

			return new SpinOutcome(
				payout,
				$"Pair {matchedFace}",
				isJackpot: false
			);
		}

		// no matches
        return new SpinOutcome(
			BigInteger.Zero,
			"Miss"
		); 
    }

    public SpinOutcome Reward(int[] results, BigInteger bet)
    {
        SpinOutcome outcome = Evaluate(results, bet);

        if (outcome.IsWin)
            Player?.Chips.AddChips(outcome.Payout); // use whatever the chip API we got

		if (printOutcome)
			LogSpinOutcome(results, bet, outcome);

        return outcome;
    }

	private static BigInteger MultiplyBigInt(BigInteger value, double multiplier)
    {
        // Multiplies accurately without floating point overflow on huge balances
        long factor = (long)(multiplier * 1000.0);
        return (value * factor) / 1000;
    }




	private void LogSpinOutcome(int[] results, BigInteger bet, SpinOutcome outcome)
	{
		string reels = results != null ? $"[{string.Join(" | ", results)}]" : "[Empty]";
		BigInteger netChange = outcome.Payout - bet;
		string netSign = netChange >= 0 ? "+" : "";
		
		if (outcome.IsJackpot)
		{
			GD.PrintRich($"[color=gold][b][JACKPOT][/b][/color] Reels: {reels} | {outcome.Description} | Bet: {bet} | Payout: {outcome.Payout:F0} (Net: [color=green]+{netChange:F0}[/color])");
		}
		else if (outcome.IsWin)
		{
			GD.PrintRich($"[color=light_green][MINI JACKPOT][/color] Reels: {reels} | {outcome.Description} | Bet: {bet} | Payout: {outcome.Payout:F0} (Net: [color=green]+{netChange:F0}[/color])");
		}
		else
		{
			GD.PrintRich($"[color=gray][MISS][/color] Reels: {reels} | {outcome.Description} | Bet: {bet} | Payout: 0 (Net: [color=red]-{bet}[/color])");
		}
    }
}