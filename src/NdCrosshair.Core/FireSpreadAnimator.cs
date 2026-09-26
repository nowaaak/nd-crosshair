namespace NdCrosshair.Core;

public sealed class FireSpreadAnimator
{
    private const double AttackSeconds = 0.03;
    private const double RecoveryTimeConstants = 3;

    private double progress;

    public bool IsResting => progress == 0;

    public int Update(bool firing, double elapsedSeconds, int maxSpread, int recoveryMilliseconds)
    {
        var target = firing ? 1.0 : 0.0;
        var timeConstant = firing
            ? AttackSeconds
            : Math.Max(recoveryMilliseconds, 1) / 1000.0 / RecoveryTimeConstants;

        progress += (target - progress) * (1 - Math.Exp(-Math.Max(elapsedSeconds, 0) / timeConstant));
        var pixels = (int)Math.Round(progress * maxSpread, MidpointRounding.AwayFromZero);
        if (maxSpread <= 0 || pixels == (int)(target * maxSpread))
        {
            progress = target;
        }

        return (int)Math.Round(progress * Math.Max(maxSpread, 0), MidpointRounding.AwayFromZero);
    }

    public void Reset() => progress = 0;
}
