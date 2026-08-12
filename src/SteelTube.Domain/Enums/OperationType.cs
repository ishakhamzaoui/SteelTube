namespace SteelTube.Domain.Enums
{
    /// <summary>
    /// Business events that affect inventory (SAD 10.1, SRS 4, SRS 6.1).
    /// Each type carries an implicit sign (SAD 67, SRS Rule "signed
    /// quantity via operation type"): Purchase and AdjustmentIncrease are
    /// positive movements; Sale and AdjustmentDecrease are negative
    /// movements. The UI never supplies a signed value directly.
    /// </summary>
    public enum OperationType
    {
        Purchase = 1,
        Sale = 2,
        AdjustmentIncrease = 3,
        AdjustmentDecrease = 4
    }
}