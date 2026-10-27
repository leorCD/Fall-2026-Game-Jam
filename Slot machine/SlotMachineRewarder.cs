using Godot;
using System;

public readonly struct SpinOutcome
{
    public double Payout {get;}
    public bool IsWin => Payout > 0;
    public string Description {get;}   // for UI / debug, e.g. "Triple 7" or "Pair"
    public bool IsJackpot {get;}

    public SpinOutcome(double payout, string description, bool isJackpot = false)
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
        { 1, 1.5 }, // ie. a full row of Face1 gives you 3x the input
		{ 2, 3 },
		{ 3, 6 },
		{ 4, 10 },
        { 5, 14 },
		{ 6, 18 },
		{ 7, 21 },
		{ 8, 25 },
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

    public SpinOutcome Evaluate(int[] results, int bet)
    {
        if (results.Length >= 3 && results[0] == results[1] && results[1] == results[2]) // all 3 faces are the same
        {
            double mult = TripleMultipliers.TryGetValue(results[0], out double m) ? m : 1.0;
            return new SpinOutcome(
				(double)bet * mult,
				$"Triple {results[0]}",
				true
			);
        }

		int matchedFace = -1; // find the matched face for pair multiplier (pairs have to be adjacent)
		if (results[0] == results[1])
			matchedFace = results[0];
		else if (results[1] == results[2])
			matchedFace = results[1];
		if (matchedFace != -1) // only 2 faces are the same
		{
			double tripleMult = TripleMultipliers.TryGetValue(matchedFace, out double m) ? m : 1.0;
			
			// Scaled down from the jackpot table, but kept at or above the minimum floor
			double pairMult = Math.Max(tripleMult * PairMultiplierFraction, MinimumPairMultiplier);

			return new SpinOutcome(
				Mathf.RoundToInt(bet * pairMult),
				$"Pair {matchedFace}",
				isJackpot: false
			);
		}
        // if (results.Length >= 3 && (results[0] == results[1] || results[1] == results[2])) // old version, doesn't consider pair multipliers
        // {
        //     return new SpinOutcome(
		// 		Mathf.RoundToInt(bet * AdjacentPairMultiplier),
		// 		"Pair"
		// 	);
        // }

        return new SpinOutcome( // no faces are the same
			0,
			"Miss"
		); 
    }

    public SpinOutcome Reward(int[] results, int bet)
    {
        SpinOutcome outcome = Evaluate(results, bet);

        if (outcome.IsWin)
            Player?.Chips.AddChips(Mathf.RoundToInt(outcome.Payout)); // use whatever the chip API we got

		if (printOutcome)
			LogSpinOutcome(results, bet, outcome);

        return outcome;
    }






	private void LogSpinOutcome(int[] results, int bet, SpinOutcome outcome)
	{
		string reels = results != null ? $"[{string.Join(" | ", results)}]" : "[Empty]";
		double netChange = outcome.Payout - bet;
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